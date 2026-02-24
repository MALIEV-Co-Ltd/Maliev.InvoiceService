using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Maliev.InvoiceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditTermEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "credit_terms",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    days = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credit_terms", x => x.code);
                });

            migrationBuilder.InsertData(
                table: "credit_terms",
                columns: new[] { "code", "days", "description", "is_active", "name" },
                values: new object[,]
                {
                    { "2/10NET30", 30, "2% discount if paid within 10 days", true, "2% 10 Net 30" },
                    { "CBD", 0, "Payment required before shipment", true, "Cash Before Delivery" },
                    { "CIA", 0, "Full payment before work begins", true, "Cash in Advance" },
                    { "COD", 0, "Payment due upon delivery", true, "Cash on Delivery" },
                    { "DEPOSIT30", 0, "30% upfront, balance on delivery", true, "30% Deposit" },
                    { "DEPOSIT50", 0, "50% upfront, balance on delivery", true, "50% Deposit" },
                    { "EOM", 30, "Due end of invoice month", true, "End of Month" },
                    { "EOM15", 45, "Due 15 days after EOM", true, "End of Month + 15" },
                    { "EOM30", 60, "Due 30 days after EOM", true, "End of Month + 30" },
                    { "MFI", 45, "Due end of month following invoice", true, "Month Following Invoice" },
                    { "MILESTONE", 0, "Payment per milestone", true, "Milestone" },
                    { "NET15", 15, "Payment due within 15 days", true, "Net 15" },
                    { "NET30", 30, "Payment due within 30 days", true, "Net 30" },
                    { "NET45", 45, "Payment due within 45 days", true, "Net 45" },
                    { "NET60", 60, "Payment due within 60 days", true, "Net 60" },
                    { "NET7", 7, "Payment due within 7 days", true, "Net 7" },
                    { "NET90", 90, "Payment due within 90 days", true, "Net 90" },
                    { "PREPAID", 0, "Full payment before invoice", true, "Prepaid" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_terms");
        }
    }
}
