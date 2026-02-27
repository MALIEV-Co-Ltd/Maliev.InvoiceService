using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InvoiceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditTermCodeToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "credit_term_code",
                table: "invoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "credit_term_code",
                table: "invoices");
        }
    }
}
