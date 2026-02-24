using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.InvoiceService.Data.Configurations;
using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Data.Data;

/// <summary>
/// Entity Framework Core DbContext for Invoice Management Service.
/// Includes manual RowVersion increment for PostgreSQL optimistic concurrency.
/// </summary>
public class InvoiceDbContext : DbContext
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor? _httpContextAccessor;

    public InvoiceDbContext(
        DbContextOptions<InvoiceDbContext> options,
        Microsoft.AspNetCore.Http.IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public InvoiceDbContext(DbContextOptions<InvoiceDbContext> options) : base(options)
    {
    }

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InvoicePaymentAllocation> InvoicePaymentAllocations => Set<InvoicePaymentAllocation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FileReference> FileReferences => Set<FileReference>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<BillingNote> BillingNotes => Set<BillingNote>();
    public DbSet<BillingNoteInvoice> BillingNoteInvoices => Set<BillingNoteInvoice>();
    public DbSet<CreditTerm> CreditTerms => Set<CreditTerm>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceLineConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());
        modelBuilder.ApplyConfiguration(new InvoicePaymentAllocationConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new FileReferenceConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());
        modelBuilder.ApplyConfiguration(new BillingNoteConfiguration());
        modelBuilder.ApplyConfiguration(new BillingNoteInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new CreditTermConfiguration());

        // Apply PostgreSQL snake_case naming convention globally
        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);
    }

    /// <summary>
    /// Override SaveChangesAsync to manually increment RowVersion for PostgreSQL.
    /// CRITICAL: PostgreSQL bytea does not auto-increment like SQL Server rowversion.
    /// This prevents false positives in concurrency tests.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Manually handle RowVersion for both new and modified invoices
        var invoiceEntries = ChangeTracker.Entries<Invoice>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in invoiceEntries)
        {
            if (entry.State == EntityState.Added)
            {
                // Initialize RowVersion for new invoices to version 1
                entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(1L);
            }
            else if (entry.State == EntityState.Modified)
            {
                // EF Core already has the OriginalValue from when the entity was loaded
                // We just need to increment the CurrentValue for the new version
                var originalVersion = entry.Property(i => i.RowVersion).OriginalValue;

                if (originalVersion == null || originalVersion.Length == 0)
                {
                    // Initialize if somehow empty (should not happen with proper initialization)
                    entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(1L);
                }
                else
                {
                    // Increment version based on the ORIGINAL value (not current)
                    var versionNumber = BitConverter.ToInt64(originalVersion, 0);
                    versionNumber++;
                    entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(versionNumber);
                }
                // Don't modify OriginalValue - EF Core needs it for the WHERE clause

            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Synchronous version of SaveChanges with manual RowVersion increment.
    /// </summary>
    public override int SaveChanges()
    {
        // Manually handle RowVersion for both new and modified invoices
        var invoiceEntries = ChangeTracker.Entries<Invoice>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        foreach (var entry in invoiceEntries)
        {
            if (entry.State == EntityState.Added)
            {
                // Initialize RowVersion for new invoices to version 1
                entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(1L);
            }
            else if (entry.State == EntityState.Modified)
            {
                // EF Core already has the OriginalValue from when the entity was loaded
                // We just need to increment the CurrentValue for the new version
                var originalVersion = entry.Property(i => i.RowVersion).OriginalValue;

                if (originalVersion == null || originalVersion.Length == 0)
                {
                    // Initialize if somehow empty (should not happen with proper initialization)
                    entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(1L);
                }
                else
                {
                    // Increment version based on the ORIGINAL value (not current)
                    var versionNumber = BitConverter.ToInt64(originalVersion, 0);
                    versionNumber++;
                    entry.Property(i => i.RowVersion).CurrentValue = BitConverter.GetBytes(versionNumber);
                }
                // Don't modify OriginalValue - EF Core needs it for the WHERE clause
            }
        }

        return base.SaveChanges();
    }
}
