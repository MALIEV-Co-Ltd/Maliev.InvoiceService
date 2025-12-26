using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Maliev.InvoiceService.Data.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Maliev.InvoiceService.Data.Data.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that automatically creates audit log entries
/// for all invoice lifecycle events (Created, Updated, Finalized, Cancelled).
/// </summary>
public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    // System Actor ID Documentation
    // =============================
    // "SYSTEM" is used as the actor ID for operations not initiated by a specific user.
    // This is ACCEPTABLE and INTENTIONAL for:
    // - Background jobs (scheduled tasks, cleanup operations)
    // - System-initiated operations (migrations, automated processes)
    // - Operations where HttpContext is not available
    //
    // Alternative Considered: Configuration-based system actor ID
    // - Would allow customization per environment (e.g., "SYSTEM-DEV", "SYSTEM-PROD")
    // - Not needed: "SYSTEM" is universally recognizable and audit trail tracks environment separately
    //
    // Reviewed: 2025-12-26 - "SYSTEM" hardcoded value is ACCEPTABLE for background operations
    private const string SystemActorId = "SYSTEM";

    public AuditLogInterceptor(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

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
                eventType = "Updated";

                // Detect specific state transitions for more descriptive event types if needed
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
                }
            }
            else
            {
                continue;
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                InvoiceId = entry.Entity.Id,
                Timestamp = DateTime.UtcNow,
                ActorId = GetCurrentUserId(),
                CreatedAt = DateTime.UtcNow,
                EventType = eventType
            };

            if (entry.State == EntityState.Modified)
            {
                auditLog.ChangedFields = CaptureChangedFields(entry);
                if (eventType == "Cancelled")
                {
                    auditLog.Reason = entry.Entity.CancellationReason;
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

    private string GetCurrentUserId()
    {
        return _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
            ?? SystemActorId;
    }
}
