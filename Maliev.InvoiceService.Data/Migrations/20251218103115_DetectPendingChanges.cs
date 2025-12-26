using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InvoiceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class DetectPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_invoices_invoice_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_file_references_invoices_invoice_id",
                table: "file_references");

            migrationBuilder.DropForeignKey(
                name: "FK_invoice_lines_invoices_invoice_id",
                table: "invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_invoice_payment_allocations_invoices_invoice_id",
                table: "invoice_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_invoices_invoices_parent_invoice_id",
                table: "invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_payments",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invoices",
                table: "invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invoice_payment_allocations",
                table: "invoice_payment_allocations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_invoice_lines",
                table: "invoice_lines");

            migrationBuilder.DropPrimaryKey(
                name: "PK_file_references",
                table: "file_references");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs");

            migrationBuilder.AddPrimaryKey(
                name: "pk_payments",
                table: "payments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_invoices",
                table: "invoices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_invoice_payment_allocations",
                table: "invoice_payment_allocations",
                columns: new[] { "invoice_id", "payment_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_invoice_lines",
                table: "invoice_lines",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_file_references",
                table: "file_references",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_audit_logs",
                table: "audit_logs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_invoices_invoice_id",
                table: "audit_logs",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_file_references_invoices_invoice_id",
                table: "file_references",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_invoice_lines_invoices_invoice_id",
                table: "invoice_lines",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_invoice_payment_allocations_invoices_invoice_id",
                table: "invoice_payment_allocations",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_invoices_invoices_parent_invoice_id",
                table: "invoices",
                column: "parent_invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_invoices_invoice_id",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_file_references_invoices_invoice_id",
                table: "file_references");

            migrationBuilder.DropForeignKey(
                name: "fk_invoice_lines_invoices_invoice_id",
                table: "invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_invoice_payment_allocations_invoices_invoice_id",
                table: "invoice_payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "fk_invoices_invoices_parent_invoice_id",
                table: "invoices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_payments",
                table: "payments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_invoices",
                table: "invoices");

            migrationBuilder.DropPrimaryKey(
                name: "pk_invoice_payment_allocations",
                table: "invoice_payment_allocations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_invoice_lines",
                table: "invoice_lines");

            migrationBuilder.DropPrimaryKey(
                name: "pk_file_references",
                table: "file_references");

            migrationBuilder.DropPrimaryKey(
                name: "pk_audit_logs",
                table: "audit_logs");

            migrationBuilder.AddPrimaryKey(
                name: "PK_payments",
                table: "payments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_invoices",
                table: "invoices",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_invoice_payment_allocations",
                table: "invoice_payment_allocations",
                columns: new[] { "invoice_id", "payment_id" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_invoice_lines",
                table: "invoice_lines",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_file_references",
                table: "file_references",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_invoices_invoice_id",
                table: "audit_logs",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_file_references_invoices_invoice_id",
                table: "file_references",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_lines_invoices_invoice_id",
                table: "invoice_lines",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_payment_allocations_invoices_invoice_id",
                table: "invoice_payment_allocations",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_invoices_parent_invoice_id",
                table: "invoices",
                column: "parent_invoice_id",
                principalTable: "invoices",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
