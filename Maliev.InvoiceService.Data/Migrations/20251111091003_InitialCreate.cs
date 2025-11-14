using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.InvoiceService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    invoice_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    parent_invoice_id = table.Column<Guid>(type: "uuid", nullable: true),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    customer_tax_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    billing_address = table.Column<string>(type: "text", nullable: false),
                    shipping_address = table.Column<string>(type: "text", nullable: true),
                    quotation_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    po_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "THB"),
                    exchange_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    exchange_rate_source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    withholding_tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    grand_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    issue_date = table.Column<DateTime>(type: "date", nullable: false),
                    due_date = table.Column<DateTime>(type: "date", nullable: false),
                    payment_terms_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    late_fee_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    finalized_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    finalized_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    cancelled_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cancellation_reason = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false, defaultValueSql: "'\\x0000000000000000'::bytea"),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoices_invoices_parent_invoice_id",
                        column: x => x.parent_invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    payment_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    payment_date = table.Column<DateTime>(type: "date", nullable: false),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reference_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    recorded_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    actor_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    changed_fields = table.Column<string>(type: "jsonb", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_number = table.Column<int>(type: "integer", nullable: false),
                    item_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    discount_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    tax_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "VAT"),
                    tax_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 7.00m),
                    line_subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoice_lines_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "idx_audit_logs_actor_id",
                table: "audit_logs",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_archived",
                table: "audit_logs",
                column: "is_archived",
                filter: "is_archived = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_invoice_id",
                table: "audit_logs",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_timestamp",
                table: "audit_logs",
                column: "timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_invoice_lines_invoice_id",
                table: "invoice_lines",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "idx_invoice_lines_invoice_line_unique",
                table: "invoice_lines",
                columns: new[] { "invoice_id", "line_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_invoice_lines_item_code",
                table: "invoice_lines",
                column: "item_code",
                filter: "item_code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_invoice_payments_invoice_amount",
                table: "invoice_payments",
                columns: new[] { "invoice_id", "allocated_amount" });

            migrationBuilder.CreateIndex(
                name: "idx_invoice_payments_payment_id",
                table: "invoice_payments",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_customer_id",
                table: "invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_customer_status_dates",
                table: "invoices",
                columns: new[] { "customer_id", "status", "issue_date" },
                descending: new[] { false, false, true },
                filter: "is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_due_date",
                table: "invoices",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_invoice_number_unique",
                table: "invoices",
                column: "invoice_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_invoices_issue_date",
                table: "invoices",
                column: "issue_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_invoices_parent_id",
                table: "invoices",
                column: "parent_invoice_id",
                filter: "parent_invoice_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_pending_payment",
                table: "invoices",
                columns: new[] { "due_date", "grand_total" },
                filter: "status IN ('Finalized', 'PartiallyPaid') AND is_deleted = FALSE");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_quotation_reference",
                table: "invoices",
                column: "quotation_reference",
                filter: "quotation_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_invoices_status",
                table: "invoices",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_payments_payment_date",
                table: "payments",
                column: "payment_date",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "idx_payments_reference_number",
                table: "payments",
                column: "reference_number",
                filter: "reference_number IS NOT NULL");

            // Create invoice_number_seq sequence for sequential invoice numbering
            migrationBuilder.Sql(@"
                CREATE SEQUENCE invoice_number_seq
                    START WITH 1
                    INCREMENT BY 1
                    NO CYCLE
                    OWNED BY NONE;
            ");

            // Create trigger function for updating updated_at timestamp
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_updated_at_column()
                RETURNS TRIGGER AS $$
                BEGIN
                    NEW.updated_at = NOW();
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Apply updated_at trigger to invoices table
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_invoices_updated_at
                BEFORE UPDATE ON invoices
                FOR EACH ROW
                EXECUTE FUNCTION update_updated_at_column();
            ");

            // Apply updated_at trigger to invoice_lines table
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_invoice_lines_updated_at
                BEFORE UPDATE ON invoice_lines
                FOR EACH ROW
                EXECUTE FUNCTION update_updated_at_column();
            ");

            // Create trigger to prevent deletion of finalized invoices
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION prevent_finalized_deletion()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF OLD.finalized_at IS NOT NULL THEN
                        RAISE EXCEPTION 'Cannot delete finalized invoice. Use cancellation instead.';
                    END IF;
                    RETURN OLD;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_prevent_finalized_deletion
                BEFORE DELETE ON invoices
                FOR EACH ROW
                EXECUTE FUNCTION prevent_finalized_deletion();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers first
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trigger_prevent_finalized_deletion ON invoices;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trigger_invoice_lines_updated_at ON invoice_lines;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trigger_invoices_updated_at ON invoices;");

            // Drop functions
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS prevent_finalized_deletion();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_updated_at_column();");

            // Drop sequence
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS invoice_number_seq;");

            // Drop tables
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "invoice_lines");

            migrationBuilder.DropTable(
                name: "invoice_payments");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "payments");
        }
    }
}
