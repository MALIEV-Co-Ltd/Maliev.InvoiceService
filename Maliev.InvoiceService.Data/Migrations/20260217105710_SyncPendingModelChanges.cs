using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InvoiceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "credit_term_code",
                table: "invoices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_credit_term_code",
                table: "invoices",
                column: "credit_term_code");

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_credit_terms_credit_term_code",
                table: "invoices",
                column: "credit_term_code",
                principalTable: "credit_terms",
                principalColumn: "code",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_invoices_credit_terms_credit_term_code",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "ix_invoices_credit_term_code",
                table: "invoices");

            migrationBuilder.AlterColumn<string>(
                name: "credit_term_code",
                table: "invoices",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
