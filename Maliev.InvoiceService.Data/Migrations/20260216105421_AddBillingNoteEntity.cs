using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InvoiceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingNoteEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "billing_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    billing_note_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    customer_tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    billing_address = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_notes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "billing_note_invoices",
                columns: table => new
                {
                    billing_note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    included_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_billing_note_invoices", x => new { x.billing_note_id, x.invoice_id });
                    table.ForeignKey(
                        name: "fk_billing_note_invoices_billing_notes_billing_note_id",
                        column: x => x.billing_note_id,
                        principalTable: "billing_notes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_billing_note_invoices_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_billing_note_invoices_invoice_id",
                table: "billing_note_invoices",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "ix_billing_notes_billing_note_number",
                table: "billing_notes",
                column: "billing_note_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "billing_note_invoices");

            migrationBuilder.DropTable(
                name: "billing_notes");
        }
    }
}
