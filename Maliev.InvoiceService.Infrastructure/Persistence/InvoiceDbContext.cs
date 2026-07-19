using Maliev.Aspire.ServiceDefaults.Database;
using Maliev.InvoiceService.Infrastructure.Persistence.Configurations;
using Maliev.InvoiceService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core DbContext for Invoice Management Service.
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

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        // Apply PostgreSQL snake_case naming convention globally
        SnakeCaseNamingHelper.ApplySnakeCaseNaming(modelBuilder);

        modelBuilder.HasSequence<int>("invoice_number_seq")
            .StartsAt(1)
            .IncrementsBy(1);
    }

}
