# Data Model: Invoice Management Service

**Feature**: 001-invoice-service
**Date**: 2025-11-11
**Database**: PostgreSQL 18 with snake_case naming convention

## Overview

This document defines the complete data model for the Invoice Management Service, including all entities, relationships, constraints, indexes, and database-level configurations. The model is designed for PostgreSQL 18 using Entity Framework Core 9.0.10 with Npgsql provider.

---

## Entity Relationship Diagram

```
┌─────────────────────────┐
│      Invoice            │
│─────────────────────────│
│ id (PK)                 │
│ invoice_number (UNIQUE) │◄────┐
│ parent_invoice_id (FK)  │─────┘ (Self-referencing for splits)
│ customer_id             │
│ customer_name           │
│ customer_tax_id         │
│ billing_address         │
│ quotation_reference     │
│ po_number               │
│ status                  │
│ currency                │
│ exchange_rate           │
│ exchange_rate_source    │
│ subtotal                │
│ tax_amount              │
│ withholding_tax_amount  │
│ grand_total             │
│ issue_date              │
│ due_date                │
│ paid_at                 │
│ finalized_at            │
│ finalized_by            │
│ cancelled_at            │
│ cancelled_by            │
│ cancellation_reason     │
│ is_deleted              │
│ row_version             │
│ created_at              │
│ updated_at              │
└─────────────────────────┘
           │ 1
           │
           │ *
┌─────────────────────────┐
│   InvoiceLine           │
│─────────────────────────│
│ id (PK)                 │
│ invoice_id (FK)         │
│ line_number             │
│ item_code               │
│ description             │
│ quantity                │
│ unit_price              │
│ discount_percentage     │
│ tax_category            │
│ tax_rate                │
│ line_subtotal           │
│ tax_amount              │
│ line_total              │
│ created_at              │
│ updated_at              │
└─────────────────────────┘

┌─────────────────────────────────┐
│ InvoicePaymentAllocation        │
│─────────────────────────────────│
│ invoice_id (PK, FK)             │
│ payment_id (PK)                 │ ◄─── Reference to Payment Service
│ allocated_amount                │
│ allocation_date                 │
│ allocation_status               │
│ allocated_by                    │
│ notes                           │
│ created_at                      │
└─────────────────────────────────┘
           │ *
           │
           │ 1
┌─────────────────────────┐
│      Invoice            │
│  (same as above)        │
└─────────────────────────┘

┌─────────────────────────┐
│      AuditLog           │
│─────────────────────────│
│ id (PK)                 │
│ invoice_id (FK)         │
│ event_type              │
│ timestamp               │
│ actor_id                │
│ changed_fields (JSONB)  │
│ reason                  │
│ is_archived             │
└─────────────────────────┘
           │ *
           │
           │ 1
┌─────────────────────────┐
│      Invoice            │
│  (same as above)        │
└─────────────────────────┘
```

---

## Entities

### 1. Invoice

**Table Name**: `invoices`

Primary entity representing a billing document issued to a customer.

| Column Name | Type | Nullable | Default | Description |
|-------------|------|----------|---------|-------------|
| `id` | `uuid` | NO | `gen_random_uuid()` | Primary key |
| `invoice_number` | `varchar(50)` | YES | NULL | Unique invoice number (e.g., INV-2025-00001), assigned on finalization |
| `parent_invoice_id` | `uuid` | YES | NULL | Foreign key to parent invoice (for split invoices) |
| `customer_id` | `uuid` | NO | - | External customer ID |
| `customer_name` | `varchar(500)` | NO | - | Customer legal name |
| `customer_tax_id` | `varchar(50)` | NO | - | Customer tax identification number |
| `billing_address` | `text` | NO | - | Full billing address |
| `shipping_address` | `text` | YES | NULL | Optional shipping address |
| `quotation_reference` | `varchar(100)` | YES | NULL | Reference to quotation system |
| `po_number` | `varchar(100)` | YES | NULL | Customer purchase order number |
| `status` | `varchar(50)` | NO | `'Draft'` | Invoice status (Draft, Finalized, Cancelled, PartiallyPaid, FullyPaid) |
| `currency` | `varchar(3)` | NO | `'THB'` | ISO 4217 currency code |
| `exchange_rate` | `decimal(18,6)` | YES | NULL | Exchange rate at creation time (if multi-currency) |
| `exchange_rate_source` | `varchar(100)` | YES | NULL | Source of exchange rate (CurrencyService, Manual, Fallback) |
| `subtotal` | `decimal(18,2)` | NO | `0` | Sum of all line subtotals before tax |
| `tax_amount` | `decimal(18,2)` | NO | `0` | Total VAT/tax amount |
| `withholding_tax_amount` | `decimal(18,2)` | NO | `0` | Withholding tax amount |
| `grand_total` | `decimal(18,2)` | NO | `0` | Final invoice total (subtotal + tax - withholding) |
| `issue_date` | `date` | NO | `CURRENT_DATE` | Date invoice was issued |
| `due_date` | `date` | NO | - | Payment due date |
| `payment_terms_days` | `int` | NO | `30` | Number of days for payment (e.g., 30, 60, 90) |
| `late_fee_percentage` | `decimal(5,2)` | YES | NULL | Late payment fee percentage |
| `paid_at` | `timestamptz` | YES | NULL | Timestamp when invoice was fully paid |
| `finalized_at` | `timestamptz` | YES | NULL | Timestamp when invoice was finalized |
| `finalized_by` | `varchar(100)` | YES | NULL | User ID who finalized the invoice |
| `cancelled_at` | `timestamptz` | YES | NULL | Timestamp when invoice was cancelled |
| `cancelled_by` | `varchar(100)` | YES | NULL | User ID who cancelled the invoice |
| `cancellation_reason` | `text` | YES | NULL | Reason for cancellation |
| `is_deleted` | `boolean` | NO | `FALSE` | Soft delete flag |
| `row_version` | `bytea` | NO | `'\\x0000000000000000'::bytea` | Optimistic concurrency token |
| `created_at` | `timestamptz` | NO | `NOW()` | Record creation timestamp |
| `updated_at` | `timestamptz` | NO | `NOW()` | Record last update timestamp |

**Constraints**:
- `PRIMARY KEY (id)`
- `UNIQUE (invoice_number)` - Invoice numbers must be unique
- `FOREIGN KEY (parent_invoice_id) REFERENCES invoices(id) ON DELETE RESTRICT` - Prevent deleting parent if children exist
- `CHECK (grand_total >= 0)` - Grand total cannot be negative
- `CHECK (status IN ('Draft', 'Finalized', 'Cancelled', 'PartiallyPaid', 'FullyPaid'))`
- `CHECK (finalized_at IS NULL OR invoice_number IS NOT NULL)` - Finalized invoices must have invoice number

**Indexes**:
- `CREATE INDEX idx_invoices_customer_id ON invoices(customer_id)` - Frequent lookups by customer
- `CREATE INDEX idx_invoices_quotation_reference ON invoices(quotation_reference) WHERE quotation_reference IS NOT NULL` - Partial index for quotations
- `CREATE INDEX idx_invoices_status ON invoices(status)` - Filter by status
- `CREATE INDEX idx_invoices_issue_date ON invoices(issue_date DESC)` - Sort by issue date
- `CREATE INDEX idx_invoices_due_date ON invoices(due_date)` - Aging reports
- `CREATE INDEX idx_invoices_parent_id ON invoices(parent_invoice_id) WHERE parent_invoice_id IS NOT NULL` - Child invoice lookups

**State Transitions**:
```
Draft → Finalized → PartiallyPaid → FullyPaid
  │        │
  └────────┴──────→ Cancelled
```

---

### 2. InvoiceLine

**Table Name**: `invoice_lines`

Represents individual line items on an invoice.

| Column Name | Type | Nullable | Default | Description |
|-------------|------|----------|---------|-------------|
| `id` | `uuid` | NO | `gen_random_uuid()` | Primary key |
| `invoice_id` | `uuid` | NO | - | Foreign key to invoice |
| `line_number` | `int` | NO | - | Line item sequence number (1, 2, 3...) |
| `item_code` | `varchar(100)` | YES | NULL | Product/service code |
| `description` | `varchar(1000)` | NO | - | Line item description |
| `quantity` | `decimal(18,4)` | NO | - | Quantity (supports fractional quantities) |
| `unit_price` | `decimal(18,2)` | NO | - | Price per unit |
| `discount_percentage` | `decimal(5,2)` | NO | `0` | Discount percentage (0-100) |
| `tax_category` | `varchar(50)` | NO | `'VAT'` | Tax category (VAT, Exempt, ZeroRated) |
| `tax_rate` | `decimal(5,2)` | NO | `7.00` | Tax rate percentage (e.g., 7% VAT in Thailand) |
| `line_subtotal` | `decimal(18,2)` | NO | - | Calculated: (quantity * unit_price) * (1 - discount/100) |
| `tax_amount` | `decimal(18,2)` | NO | - | Calculated: line_subtotal * (tax_rate / 100) |
| `line_total` | `decimal(18,2)` | NO | - | Calculated: line_subtotal + tax_amount |
| `created_at` | `timestamptz` | NO | `NOW()` | Record creation timestamp |
| `updated_at` | `timestamptz` | NO | `NOW()` | Record last update timestamp |

**Constraints**:
- `PRIMARY KEY (id)`
- `FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE` - Delete lines when invoice deleted
- `UNIQUE (invoice_id, line_number)` - Line numbers unique within invoice
- `CHECK (quantity > 0)` - Quantity must be positive
- `CHECK (unit_price >= 0)` - Unit price cannot be negative
- `CHECK (discount_percentage >= 0 AND discount_percentage <= 100)`
- `CHECK (tax_rate >= 0 AND tax_rate <= 100)`

**Indexes**:
- `CREATE INDEX idx_invoice_lines_invoice_id ON invoice_lines(invoice_id)` - Join with invoice
- `CREATE INDEX idx_invoice_lines_item_code ON invoice_lines(item_code) WHERE item_code IS NOT NULL` - Product analytics

**Calculation Logic** (enforced in application):
```
line_subtotal = (quantity * unit_price) * (1 - discount_percentage / 100)
tax_amount = line_subtotal * (tax_rate / 100)
line_total = line_subtotal + tax_amount
```

---

### 3. InvoicePaymentAllocation

**Table Name**: `invoice_payment_allocations`

**Purpose**: Tracks allocation of payments (from Payment Service) to invoices. This table stores references to payment IDs from the Payment Service and does not own payment transaction data.

**Architecture Note**: Payment processing and transaction data is owned by the Payment Service. This table only stores allocation records linking Payment Service payment IDs to invoices within this service.

| Column Name | Type | Nullable | Default | Description |
|-------------|------|----------|---------|-------------|
| `invoice_id` | `uuid` | NO | - | Foreign key to invoice |
| `payment_id` | `uuid` | NO | - | Reference to payment in Payment Service (NO FK constraint) |
| `allocated_amount` | `decimal(18,2)` | NO | - | Amount of payment allocated to this invoice |
| `allocation_date` | `timestamptz` | NO | `NOW()` | Date/time when payment was allocated |
| `allocation_status` | `varchar(20)` | NO | `'Confirmed'` | Status: Confirmed, Reversed |
| `allocated_by` | `varchar(100)` | NO | - | User ID who performed the allocation |
| `notes` | `text` | YES | NULL | Additional allocation notes |
| `created_at` | `timestamptz` | NO | `NOW()` | Record creation timestamp |

**Constraints**:
- `PRIMARY KEY (invoice_id, payment_id)`
- `FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE`
- `CHECK (allocated_amount > 0)` - Allocated amount must be positive
- `CHECK (allocation_status IN ('Confirmed', 'Reversed'))`

**Indexes**:
- `CREATE INDEX idx_invoice_payment_allocations_payment_id ON invoice_payment_allocations(payment_id)` - Lookup allocations by payment
- `CREATE INDEX idx_invoice_payment_allocations_status ON invoice_payment_allocations(allocation_status) WHERE allocation_status = 'Confirmed'` - Filter confirmed allocations

**Business Rules** (enforced in application):
- Payment must be validated against Payment Service API before allocation
- Payment must have status "Succeeded" in Payment Service
- Invoice status updated to `PartiallyPaid` or `FullyPaid` based on total confirmed allocations
- Outstanding balance calculated as: `invoice.grand_total - SUM(confirmed_allocations)`

**Integration Notes**:
- Before creating allocation, Invoice Service calls Payment Service API: `GET /payments/v1/payments/{id}`
- Payment Service events (`PaymentSucceededEvent`, `PaymentRefundedEvent`) trigger allocation workflows via RabbitMQ

---

### 4. AuditLog

**Table Name**: `audit_logs`

Comprehensive audit trail for all invoice lifecycle events.

| Column Name | Type | Nullable | Default | Description |
|-------------|------|----------|---------|-------------|
| `id` | `uuid` | NO | `gen_random_uuid()` | Primary key |
| `invoice_id` | `uuid` | NO | - | Foreign key to invoice |
| `event_type` | `varchar(50)` | NO | - | Event type (Created, Updated, Finalized, Cancelled, PaymentLinked) |
| `timestamp` | `timestamptz` | NO | `NOW()` | Event timestamp |
| `actor_id` | `varchar(100)` | NO | - | User ID or system identifier |
| `changed_fields` | `jsonb` | YES | NULL | JSON object of changed field names and new values (for Update events) |
| `reason` | `text` | YES | NULL | Reason for action (e.g., cancellation reason) |
| `is_archived` | `boolean` | NO | `FALSE` | Flag indicating if log has been archived to cold storage |
| `created_at` | `timestamptz` | NO | `NOW()` | Record creation timestamp |

**Constraints**:
- `PRIMARY KEY (id)`
- `FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE RESTRICT` - Never delete audit logs
- `CHECK (event_type IN ('Created', 'Updated', 'Finalized', 'Cancelled', 'PaymentLinked', 'Split'))`

**Indexes**:
- `CREATE INDEX idx_audit_logs_invoice_id ON audit_logs(invoice_id)` - Retrieve all events for an invoice
- `CREATE INDEX idx_audit_logs_timestamp ON audit_logs(timestamp DESC)` - Chronological queries
- `CREATE INDEX idx_audit_logs_actor_id ON audit_logs(actor_id)` - User activity tracking
- `CREATE INDEX idx_audit_logs_archived ON audit_logs(is_archived) WHERE is_archived = FALSE` - Hot storage queries

**Retention Policy**:
- Logs older than 1 year: Marked `is_archived = TRUE` and optionally exported to Google Cloud Storage
- Logs retained for minimum 7 years per regulatory requirement (FR-056)

**Example `changed_fields` JSON**:
```json
{
  "customer_name": "Old Corp Ltd. → New Corp Ltd.",
  "grand_total": "50000.00 → 55000.00",
  "due_date": "2025-01-15 → 2025-02-15"
}
```

---

## Database Sequences

### invoice_number_seq

**Purpose**: Generate sequential invoice numbers.

```sql
CREATE SEQUENCE invoice_number_seq
    START WITH 1
    INCREMENT BY 1
    NO CYCLE
    OWNED BY NONE;
```

**Usage Pattern**:
1. Application retrieves next value: `SELECT nextval('invoice_number_seq')`
2. Format: `INV-{year}-{seq:D5}` (e.g., INV-2025-00001)
3. Assign to `invoice.invoice_number` during finalization
4. Sequence persists across transaction rollbacks (prevents number reuse)

---

## Composite Indexes for Query Optimization

```sql
-- Multi-column index for customer invoice searches with date range
CREATE INDEX idx_invoices_customer_status_dates
ON invoices(customer_id, status, issue_date DESC)
WHERE is_deleted = FALSE;

-- Partial index for pending payment invoices (aging reports)
CREATE INDEX idx_invoices_pending_payment
ON invoices(due_date, grand_total)
WHERE status IN ('Finalized', 'PartiallyPaid') AND is_deleted = FALSE;

-- Composite index for payment allocation lookups
CREATE INDEX idx_invoice_payments_invoice_amount
ON invoice_payments(invoice_id, allocated_amount);
```

---

## Database Functions and Triggers

### 1. Update `updated_at` Timestamp

```sql
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_invoices_updated_at
BEFORE UPDATE ON invoices
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER trigger_invoice_lines_updated_at
BEFORE UPDATE ON invoice_lines
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();
```

### 2. Prevent Deletion of Finalized Invoices

```sql
CREATE OR REPLACE FUNCTION prevent_finalized_invoice_deletion()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.status IN ('Finalized', 'PartiallyPaid', 'FullyPaid', 'Cancelled') THEN
        RAISE EXCEPTION 'Cannot delete finalized or cancelled invoices (ID: %)', OLD.id;
    END IF;
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_prevent_finalized_deletion
BEFORE DELETE ON invoices
FOR EACH ROW
EXECUTE FUNCTION prevent_finalized_invoice_deletion();
```

---

## Entity Framework Core Configurations

### Invoice Configuration

```csharp
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property(i => i.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(50);

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique()
            .HasFilter("invoice_number IS NOT NULL");

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(i => i.Subtotal)
            .HasColumnName("subtotal")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.GrandTotal)
            .HasColumnName("grand_total")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.RowVersion)
            .HasColumnName("row_version")
            .IsRowVersion()
            .ValueGeneratedNever()  // CRITICAL for PostgreSQL
            .HasDefaultValueSql("'\\x0000000000000000'::bytea")
            .IsRequired();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(i => i.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Self-referencing relationship for parent-child invoices
        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(i => i.ParentInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationship with InvoiceLines
        builder.HasMany(i => i.Lines)
            .WithOne()
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(i => i.CustomerId).HasDatabaseName("idx_invoices_customer_id");
        builder.HasIndex(i => i.Status).HasDatabaseName("idx_invoices_status");
        builder.HasIndex(i => i.IssueDate).HasDatabaseName("idx_invoices_issue_date").IsDescending();
    }
}
```

### InvoiceLine Configuration

```csharp
public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property(l => l.InvoiceId).HasColumnName("invoice_id").IsRequired();
        builder.Property(l => l.LineNumber).HasColumnName("line_number").IsRequired();

        builder.HasIndex(l => new { l.InvoiceId, l.LineNumber })
            .IsUnique()
            .HasDatabaseName("uq_invoice_lines_invoice_line_number");

        builder.Property(l => l.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(l => l.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(l => l.LineSubtotal)
            .HasColumnName("line_subtotal")
            .HasColumnType("decimal(18,2)")
            .IsRequired();
    }
}
```

### Payment and InvoicePayment Configurations

```csharp
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.PaymentAmount)
            .HasColumnName("payment_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.PaymentMethod)
            .HasColumnName("payment_method")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();
    }
}

public class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.ToTable("invoice_payments");

        builder.HasKey(ip => new { ip.InvoiceId, ip.PaymentId });

        builder.Property(ip => ip.AllocatedAmount)
            .HasColumnName("allocated_amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(ip => ip.Invoice)
            .WithMany()
            .HasForeignKey(ip => ip.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ip => ip.Payment)
            .WithMany(p => p.InvoicePayments)
            .HasForeignKey(ip => ip.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## Migration Strategy

### Initial Migration

```bash
# Set environment variable for design-time DbContext
export ConnectionStrings__InvoiceDbContext="Server=localhost;Port=5432;Database=invoice_dev_db;Username=postgres;Password=<dev-password>;"

# Create initial migration
dotnet ef migrations add InitialCreate --project Maliev.InvoiceService.Infrastructure --startup-project Maliev.InvoiceService.Infrastructure

# Apply migration to local database
dotnet ef database update --project Maliev.InvoiceService.Infrastructure --startup-project Maliev.InvoiceService.Infrastructure
```

### Kubernetes Database Migration

```bash
# Port-forward to PostgreSQL in Kubernetes
kubectl port-forward -n maliev-dev postgres-cluster-1 5432:5432

# Set connection string with actual credentials
export ConnectionStrings__InvoiceDbContext="Server=localhost;Port=5432;Database=invoice_app_db;Username=postgres;Password=<actual-password>;"

# Apply migrations
dotnet ef database update --project Maliev.InvoiceService.Infrastructure --startup-project Maliev.InvoiceService.Infrastructure
```

---

## Data Integrity Rules Summary

| Rule | Enforcement |
|------|------------|
| Invoice number uniqueness | Database unique constraint + sequence |
| Optimistic concurrency | Manual RowVersion increment in SaveChanges |
| Finalized invoices immutable | Application-level validation before update |
| Prevent deletion of finalized invoices | Database trigger |
| Payment allocation <= payment amount | Application-level validation |
| Sum of child invoice totals = parent total | Application-level validation during split |
| Audit log immutability | Foreign key with ON DELETE RESTRICT |
| Line item calculations deterministic | Computed in application, stored in database |
| 7-year audit log retention | Background service archival process |

---

## Example Queries

### 1. Get Invoice with Lines and Payments
```sql
SELECT i.*, il.*, ip.*, p.*
FROM invoices i
LEFT JOIN invoice_lines il ON il.invoice_id = i.id
LEFT JOIN invoice_payments ip ON ip.invoice_id = i.id
LEFT JOIN payments p ON p.id = ip.payment_id
WHERE i.id = 'invoice-uuid'
AND i.is_deleted = FALSE
ORDER BY il.line_number;
```

### 2. Aging Report (Outstanding Invoices)
```sql
SELECT
    i.invoice_number,
    i.customer_name,
    i.issue_date,
    i.due_date,
    i.grand_total,
    COALESCE(SUM(ip.allocated_amount), 0) AS total_paid,
    i.grand_total - COALESCE(SUM(ip.allocated_amount), 0) AS outstanding,
    CURRENT_DATE - i.due_date AS days_overdue
FROM invoices i
LEFT JOIN invoice_payments ip ON ip.invoice_id = i.id
WHERE i.status IN ('Finalized', 'PartiallyPaid')
AND i.is_deleted = FALSE
GROUP BY i.id
HAVING i.grand_total > COALESCE(SUM(ip.allocated_amount), 0)
ORDER BY days_overdue DESC;
```

### 3. Audit Trail for Invoice
```sql
SELECT
    al.event_type,
    al.timestamp,
    al.actor_id,
    al.changed_fields,
    al.reason
FROM audit_logs al
WHERE al.invoice_id = 'invoice-uuid'
AND al.is_archived = FALSE
ORDER BY al.timestamp DESC;
```

---

## Performance Considerations

1. **Index Coverage**: All foreign keys have indexes for join performance
2. **Partial Indexes**: Used for nullable columns (quotation_reference, parent_invoice_id) to reduce index size
3. **AsNoTracking**: Applied to all read-only queries for ~30% performance improvement
4. **Computed Columns**: Line totals and invoice totals stored (not computed on-the-fly) for fast aggregations
5. **Pagination**: All list queries paginated (max 1000 records per page)
6. **Audit Log Archival**: Background service moves old logs to cold storage to keep hot storage performant

---

**Status**: ✅ Data Model Complete
**Next Step**: Generate API contracts in `/contracts/` directory
