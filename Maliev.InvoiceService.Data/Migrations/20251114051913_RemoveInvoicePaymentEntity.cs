using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InvoiceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvoicePaymentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_payments");

            migrationBuilder.AddColumn<string>(
                name: "pdf_file_reference",
                table: "invoices",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "invoice_payment_allocations",
                columns: table => new
                {
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    allocation_date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    allocation_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Confirmed"),
                    allocated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "system"),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_payment_allocations", x => new { x.invoice_id, x.payment_id });
                    table.ForeignKey(
                        name: "FK_invoice_payment_allocations_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_invoice_payment_allocations_invoice_status",
                table: "invoice_payment_allocations",
                columns: new[] { "invoice_id", "allocation_status" });

            migrationBuilder.CreateIndex(
                name: "idx_invoice_payment_allocations_payment_id",
                table: "invoice_payment_allocations",
                column: "payment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_payment_allocations");

            migrationBuilder.DropColumn(
                name: "pdf_file_reference",
                table: "invoices");

            migrationBuilder.CreateTable(
                name: "invoice_payments",
                columns: table => new
                {
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocated_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_payments", x => new { x.invoice_id, x.payment_id });
                    table.ForeignKey(
                        name: "FK_invoice_payments_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_payments_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_invoice_payments_invoice_amount",
                table: "invoice_payments",
                columns: new[] { "invoice_id", "allocated_amount" });

            migrationBuilder.CreateIndex(
                name: "idx_invoice_payments_payment_id",
                table: "invoice_payments",
                column: "payment_id");
        }
    }
}
