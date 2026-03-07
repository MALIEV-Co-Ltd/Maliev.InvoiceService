using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InvoiceService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "invoice_number_seq");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "invoice_number_seq");
        }
    }
}
