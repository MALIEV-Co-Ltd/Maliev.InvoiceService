namespace Maliev.InvoiceService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Invoice Service.
/// </summary>
public static class InvoicePermissions
{
    public const string InvoiceCreate = "invoice.invoices.create";
    public const string InvoiceRead = "invoice.invoices.read";
    public const string InvoiceUpdate = "invoice.invoices.update";
    public const string InvoiceDelete = "invoice.invoices.delete";
    public const string InvoiceFinalize = "invoice.invoices.finalize";
    public const string InvoiceApprove = "invoice.invoices.approve";
    public const string InvoiceVoid = "invoice.invoices.void";
    public const string InvoiceExport = "invoice.invoices.export";
    public const string InvoiceSend = "invoice.invoices.send";

    public const string SegmentCreate = "invoice.segments.create";
    public const string SegmentRead = "invoice.segments.read";
    public const string SegmentUpdate = "invoice.segments.update";
    public const string SegmentDelete = "invoice.segments.delete";

    public const string SplitCreate = "invoice.splits.create";
    public const string SplitManage = "invoice.splits.manage";

    public const string FileUpload = "invoice.files.upload";
    public const string FileRead = "invoice.files.read";
    public const string FileDelete = "invoice.files.delete";
    public const string FileRegister = "invoice.files.register";

    public const string ReportCurrency = "invoice.reports.currency";
    public const string ReportAnalytics = "invoice.reports.analytics";
    public const string ReportExport = "invoice.reports.export";

    public const string BillingNoteCreate = "invoice.billing-notes.create";
    public const string BillingNoteRead = "invoice.billing-notes.read";
    public const string BillingNoteUpdate = "invoice.billing-notes.update";
    public const string BillingNoteDelete = "invoice.billing-notes.delete";

    public const string CreditTermRead = "invoice.credit-terms.read";
    public const string CreditTermManage = "invoice.credit-terms.manage";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { InvoiceCreate, "Create invoices" },
        { InvoiceRead, "Read invoices" },
        { InvoiceUpdate, "Update invoices" },
        { InvoiceDelete, "Delete invoices" },
        { InvoiceFinalize, "Finalize invoices" },
        { InvoiceApprove, "Approve invoices" },
        { InvoiceVoid, "Void invoices" },
        { InvoiceExport, "Export invoices" },
        { InvoiceSend, "Send invoices" },
        { SegmentCreate, "Create invoice segments" },
        { SegmentRead, "Read invoice segments" },
        { SegmentUpdate, "Update invoice segments" },
        { SegmentDelete, "Delete invoice segments" },
        { SplitCreate, "Create invoice splits" },
        { SplitManage, "Manage invoice splits" },
        { FileUpload, "Upload invoice files" },
        { FileRead, "Read invoice files" },
        { FileDelete, "Delete invoice files" },
        { FileRegister, "Register invoice files" },
        { ReportCurrency, "Generate currency reports" },
        { ReportAnalytics, "Generate analytics reports" },
        { ReportExport, "Export reports" },
        { BillingNoteCreate, "Create billing notes" },
        { BillingNoteRead, "Read billing notes" },
        { BillingNoteUpdate, "Update billing notes" },
        { BillingNoteDelete, "Delete billing notes" },
        { CreditTermRead, "Read credit terms" },
        { CreditTermManage, "Manage credit terms" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
