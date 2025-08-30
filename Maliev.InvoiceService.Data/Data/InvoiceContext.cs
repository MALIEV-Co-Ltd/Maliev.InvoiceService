using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

using Maliev.InvoiceService.Data.Models;

namespace Maliev.InvoiceService.Data.Data
{
    /// <summary>
    /// Represents the database context for the InvoiceService.
    /// </summary>
    public partial class InvoiceContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceContext"/> class.
        /// </summary>
        public InvoiceContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceContext"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public InvoiceContext(DbContextOptions<InvoiceContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the Invoices DbSet.
        /// </summary>
        public virtual DbSet<Invoice> Invoices { get; set; }
        /// <summary>
        /// Gets or sets the InvoiceFiles DbSet.
        /// </summary>
        public virtual DbSet<InvoiceFile> InvoiceFiles { get; set; }
        /// <summary>
        /// Gets or sets the OrderItems DbSet.
        /// </summary>
        public virtual DbSet<OrderItem> OrderItems { get; set; }

        /// <summary>
        /// Configures the model that was discovered by convention from the entity types
        /// exposed in <see cref="T:Microsoft.EntityFrameworkCore.DbSet`1" /> properties on this context.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.ToTable("Invoice");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.BillingAddressBuilding).HasMaxLength(256);

                entity.Property(e => e.BillingAddressCity).HasMaxLength(256);

                entity.Property(e => e.BillingAddressCompany).HasMaxLength(256);

                entity.Property(e => e.BillingAddressCountry).HasMaxLength(256);

                entity.Property(e => e.BillingAddressLine1).HasMaxLength(256);

                entity.Property(e => e.BillingAddressLine2).HasMaxLength(256);

                entity.Property(e => e.BillingAddressCity).HasMaxLength(256);

                entity.Property(e => e.BillingAddressState).HasMaxLength(256);

                entity.Property(e => e.BillingAddressPostalCode).HasMaxLength(256);

                entity.Property(e => e.BillingAddressRecipient).HasMaxLength(256);

                entity.Property(e => e.BillingAddressState).HasMaxLength(256);

                entity.Property(e => e.CommercialRegistration).HasMaxLength(256);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Currency).HasMaxLength(50);

                entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

                entity.Property(e => e.Fob)
                    .HasMaxLength(256)
                    .HasColumnName("FOB");

                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Number)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Outstanding).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.PaymentDate).HasColumnType("datetime");

                entity.Property(e => e.PurchaseOrderNumber).HasMaxLength(256);

                entity.Property(e => e.ReceiptId).HasColumnName("ReceiptID");

                entity.Property(e => e.Requisitioner).HasMaxLength(256);

                entity.Property(e => e.SalesPerson).HasMaxLength(256);

                entity.Property(e => e.ShippedVia).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressBuilding).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressCity).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressCompany).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressCountry).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressLine1).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressLine2).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressPostalCode).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressRecipient).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressRecipientTelephone).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressCompany).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressBuilding).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressLine1).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressLine2).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressCity).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressState).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressPostalCode).HasMaxLength(256);

                entity.Property(e => e.ShippingAddressCountry).HasMaxLength(256);

                entity.Property(e => e.CommercialRegistration).HasMaxLength(256);

                entity.Property(e => e.TaxIdentification).HasMaxLength(100);

                entity.Property(e => e.Terms).HasMaxLength(256);

                entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");

                entity.Property(e => e.Vat)
                    .HasColumnType("decimal(18, 2)")
                    .HasColumnName("VAT");

                entity.Property(e => e.WithholdingTax).HasColumnType("decimal(18, 2)");
            });

            modelBuilder.Entity<InvoiceFile>(entity =>
            {
                entity.ToTable("InvoiceFile");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Bucket)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");

                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.ObjectName).IsRequired();

                entity.HasOne(d => d.Invoice)
                    .WithMany(p => p.InvoiceFiles)
                    .HasForeignKey(d => d.InvoiceId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_InvoiceFile_Invoice");
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItem");

                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.InvoiceId).HasColumnName("InvoiceID");

                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Subtotal)
                    .HasColumnType("decimal(18, 2)")
                    .HasComputedColumnSql("(CONVERT([decimal](18,2),[UnitPrice]*[Quantity]))", false);

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

                entity.HasOne(d => d.Invoice)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(d => d.InvoiceId)
                    .HasConstraintName("FK_OrderItem_Invoice");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
