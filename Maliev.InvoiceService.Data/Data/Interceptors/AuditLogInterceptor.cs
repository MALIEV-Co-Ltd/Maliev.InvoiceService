using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Maliev.InvoiceService.Data.Models;
using System.Text.Json;

namespace Maliev.InvoiceService.Data.Data.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that automatically creates audit log entries
/// for all invoice lifecycle events (Created, Updated, Finalized, Cancelled).
/// </summary>
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private const string SystemActorId = "SYSTEM";

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CreateAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void CreateAuditLogs(DbContext? context)
    {
        if (context == null) return;

        var invoiceEntries = context.ChangeTracker.Entries<Invoice>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        // Get existing audit logs already tracked in this context to prevent duplicates
        var existingAuditLogs = context.ChangeTracker.Entries<AuditLog>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => new { e.Entity.InvoiceId, e.Entity.EventType })
            .ToHashSet();

        foreach (var entry in invoiceEntries)
        {
            string eventType;

            if (entry.State == EntityState.Added)
            {
                eventType = "Created";
            }
            else if (entry.State == EntityState.Modified)
            {
                // Detect specific state transitions
                var statusProperty = entry.Property(nameof(Invoice.Status));
                if (statusProperty.IsModified)
                {
                    var originalStatus = statusProperty.OriginalValue?.ToString();
                    var currentStatus = statusProperty.CurrentValue?.ToString();

                    if (currentStatus == "Finalized" && originalStatus == "Draft")
                    {
                        eventType = "Finalized";
                    }
                    else if (currentStatus == "Cancelled")
                    {
                        eventType = "Cancelled";
                    }
                    else
                    {
                        eventType = "Updated";
                    }
                }
                else
                {
                    eventType = "Updated";
                }
            }
            else
            {
                continue;
            }

            // Skip if audit log already exists for this invoice and event type
            if (existingAuditLogs.Contains(new { InvoiceId = entry.Entity.Id, EventType = eventType }))
                continue;

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                InvoiceId = entry.Entity.Id,
                Timestamp = DateTime.UtcNow,
                ActorId = SystemActorId, // TODO: Get from HttpContext claims in production
                CreatedAt = DateTime.UtcNow,
                EventType = eventType
            };

            if (entry.State == EntityState.Modified)
            {
                // Populate changed fields
                var statusProperty = entry.Property(nameof(Invoice.Status));
                if (statusProperty.IsModified)
                {
                    var originalStatus = statusProperty.OriginalValue?.ToString();
                    var currentStatus = statusProperty.CurrentValue?.ToString();

                    if (currentStatus == "Finalized" && originalStatus == "Draft")
                    {
                        auditLog.ChangedFields = JsonSerializer.Serialize(new Dictionary<string, string>
                        {
                            ["status"] = $"{originalStatus} → {currentStatus}",
                            ["invoice_number"] = entry.Property(nameof(Invoice.InvoiceNumber)).CurrentValue?.ToString() ?? ""
                        });
                    }
                    else if (currentStatus == "Cancelled")
                    {
                        auditLog.Reason = entry.Entity.CancellationReason;
                        auditLog.ChangedFields = JsonSerializer.Serialize(new Dictionary<string, string>
                        {
                            ["status"] = $"{originalStatus} → {currentStatus}"
                        });
                    }
                    else
                    {
                        auditLog.ChangedFields = CaptureChangedFields(entry) ?? "{}";
                    }
                }
                else
                {
                    auditLog.ChangedFields = CaptureChangedFields(entry);
                }
            }

            context.Set<AuditLog>().Add(auditLog);
        }
    }

    private string? CaptureChangedFields(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<Invoice> entry)
    {
        var changes = new Dictionary<string, string>();

        foreach (var property in entry.Properties)
        {
            if (property.IsModified &&
                property.Metadata.Name != nameof(Invoice.UpdatedAt) &&
                property.Metadata.Name != nameof(Invoice.RowVersion))
            {
                var originalValue = property.OriginalValue?.ToString() ?? "NULL";
                var currentValue = property.CurrentValue?.ToString() ?? "NULL";

                if (originalValue != currentValue)
                {
                    changes[property.Metadata.Name] = $"{originalValue} → {currentValue}";
                }
            }
        }

        return changes.Count > 0 ? JsonSerializer.Serialize(changes) : null;
    }
}
