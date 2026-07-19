using Maliev.InvoiceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maliev.InvoiceService.Infrastructure.Persistence.Configurations;

public class CreditTermConfiguration : IEntityTypeConfiguration<CreditTerm>
{
    public void Configure(EntityTypeBuilder<CreditTerm> builder)
    {
        builder.HasKey(c => c.Code);

        builder.Property(c => c.Code)
            .HasMaxLength(20);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        // Seed Data
        builder.HasData(
            new CreditTerm { Code = "COD", Name = "Cash on Delivery", Days = 0, Description = "Payment due upon delivery" },
            new CreditTerm { Code = "NET7", Name = "Net 7", Days = 7, Description = "Payment due within 7 days" },
            new CreditTerm { Code = "NET15", Name = "Net 15", Days = 15, Description = "Payment due within 15 days" },
            new CreditTerm { Code = "NET30", Name = "Net 30", Days = 30, Description = "Payment due within 30 days" },
            new CreditTerm { Code = "NET45", Name = "Net 45", Days = 45, Description = "Payment due within 45 days" },
            new CreditTerm { Code = "NET60", Name = "Net 60", Days = 60, Description = "Payment due within 60 days" },
            new CreditTerm { Code = "NET90", Name = "Net 90", Days = 90, Description = "Payment due within 90 days" },
            new CreditTerm { Code = "2/10NET30", Name = "2% 10 Net 30", Days = 30, Description = "2% discount if paid within 10 days" },
            new CreditTerm { Code = "EOM", Name = "End of Month", Days = 30, Description = "Due end of invoice month" },
            new CreditTerm { Code = "EOM15", Name = "End of Month + 15", Days = 45, Description = "Due 15 days after EOM" },
            new CreditTerm { Code = "EOM30", Name = "End of Month + 30", Days = 60, Description = "Due 30 days after EOM" },
            new CreditTerm { Code = "MFI", Name = "Month Following Invoice", Days = 45, Description = "Due end of month following invoice" },
            new CreditTerm { Code = "DEPOSIT50", Name = "50% Deposit", Days = 0, Description = "50% upfront, balance on delivery" },
            new CreditTerm { Code = "DEPOSIT30", Name = "30% Deposit", Days = 0, Description = "30% upfront, balance on delivery" },
            new CreditTerm { Code = "MILESTONE", Name = "Milestone", Days = 0, Description = "Payment per milestone" },
            new CreditTerm { Code = "CIA", Name = "Cash in Advance", Days = 0, Description = "Full payment before work begins" },
            new CreditTerm { Code = "CBD", Name = "Cash Before Delivery", Days = 0, Description = "Payment required before shipment" },
            new CreditTerm { Code = "PREPAID", Name = "Prepaid", Days = 0, Description = "Full payment before invoice" }
        );
    }
}
