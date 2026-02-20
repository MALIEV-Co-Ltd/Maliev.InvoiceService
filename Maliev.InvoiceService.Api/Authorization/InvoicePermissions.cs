namespace Maliev.InvoiceService.Api.Authorization;

/// <summary>
/// Defines all permissions for the Invoice Service.
/// Permissions follow the format: invoice.{resource}.{action}
/// </summary>
public static class InvoicePermissions
{
    // Invoice Operations
    /// <summary>Permission to create new invoices.</summary>
    public const string InvoicesCreate = "invoice.invoices.create";
    /// <summary>Permission to read invoice details.</summary>
    public const string InvoicesRead = "invoice.invoices.read";
    /// <summary>Permission to update invoice information.</summary>
    public const string InvoicesUpdate = "invoice.invoices.update";
    /// <summary>Permission to delete invoices.</summary>
    public const string InvoicesDelete = "invoice.invoices.delete";
    /// <summary>Permission to finalize invoices (lock for editing).</summary>
    public const string InvoicesFinalize = "invoice.invoices.finalize";
    /// <summary>Permission to approve invoices.</summary>
    public const string InvoicesApprove = "invoice.invoices.approve";
    /// <summary>Permission to void/cancel invoices.</summary>
    public const string InvoicesVoid = "invoice.invoices.void";
    /// <summary>Permission to export invoices to various formats.</summary>
    public const string InvoicesExport = "invoice.invoices.export";
    /// <summary>Permission to send invoices to customers.</summary>
    public const string InvoicesSend = "invoice.invoices.send";

    // Segment Operations
    /// <summary>Permission to create invoice segments.</summary>
    public const string SegmentsCreate = "invoice.segments.create";
    /// <summary>Permission to read segment details.</summary>
    public const string SegmentsRead = "invoice.segments.read";
    /// <summary>Permission to update segments.</summary>
    public const string SegmentsUpdate = "invoice.segments.update";
    /// <summary>Permission to delete segments.</summary>
    public const string SegmentsDelete = "invoice.segments.delete";

    // Split Operations
    /// <summary>Permission to create split invoices.</summary>
    public const string SplitsCreate = "invoice.splits.create";
    /// <summary>Permission to manage split invoice relationships.</summary>
    public const string SplitsManage = "invoice.splits.manage";

    // File Operations
    /// <summary>Permission to upload files to invoices.</summary>
    public const string FilesUpload = "invoice.files.upload";
    /// <summary>Permission to read/download invoice files.</summary>
    public const string FilesRead = "invoice.files.read";
    /// <summary>Permission to delete invoice files.</summary>
    public const string FilesDelete = "invoice.files.delete";
    /// <summary>Permission to register PDF files (service-to-service).</summary>
    public const string FilesRegister = "invoice.files.register";

    // Reporting Operations
    /// <summary>Permission to view currency exchange reports.</summary>
    public const string ReportsCurrency = "invoice.reports.currency";
    /// <summary>Permission to access invoice analytics.</summary>
    public const string ReportsAnalytics = "invoice.reports.analytics";
    /// <summary>Permission to export reports.</summary>
    public const string ReportsExport = "invoice.reports.export";

    // Billing Note Operations
    /// <summary>Permission to create billing notes.</summary>
    public const string BillingNotesCreate = "invoice.billing-notes.create";
    /// <summary>Permission to read billing notes.</summary>
    public const string BillingNotesRead = "invoice.billing-notes.read";
    /// <summary>Permission to update billing notes.</summary>
    public const string BillingNotesUpdate = "invoice.billing-notes.update";
    /// <summary>Permission to delete billing notes.</summary>
    public const string BillingNotesDelete = "invoice.billing-notes.delete";

    // Credit Term Operations
    /// <summary>Permission to read credit terms.</summary>
    public const string CreditTermsRead = "invoice.credit-terms.read";
    /// <summary>Permission to manage credit terms.</summary>
    public const string CreditTermsManage = "invoice.credit-terms.manage";

    /// <summary>
    /// Collection of all defined invoice permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { InvoicesCreate, "Create new invoices" },
        { InvoicesRead, "Read invoice details" },
        { InvoicesUpdate, "Update invoice information" },
        { InvoicesDelete, "Delete invoices" },
        { InvoicesFinalize, "Finalize invoices (lock for editing)" },
        { InvoicesApprove, "Approve invoices" },
        { InvoicesVoid, "Void/cancel invoices" },
        { InvoicesExport, "Export invoices to various formats" },
        { InvoicesSend, "Send invoices to customers" },
        { SegmentsCreate, "Create invoice segments" },
        { SegmentsRead, "Read segment details" },
        { SegmentsUpdate, "Update segments" },
        { SegmentsDelete, "Delete segments" },
        { SplitsCreate, "Create split invoices" },
        { SplitsManage, "Manage split invoice relationships" },
        { FilesUpload, "Upload files to invoices" },
        { FilesRead, "Read/download invoice files" },
        { FilesDelete, "Delete invoice files" },
        { FilesRegister, "Register PDF files (service-to-service)" },
        { ReportsCurrency, "View currency exchange reports" },
        { ReportsAnalytics, "Access invoice analytics" },
        { ReportsExport, "Export reports" },
        { BillingNotesCreate, "Create billing notes" },
        { BillingNotesRead, "Read billing notes" },
        { BillingNotesUpdate, "Update billing notes" },
        { BillingNotesDelete, "Delete billing notes" },
        { CreditTermsRead, "Read credit terms" },
        { CreditTermsManage, "Manage credit terms" }
    };

    /// <summary>
    /// All permissions defined for the Invoice Service.
    /// </summary>
    public static readonly string[] All = AllWithDescriptions.Keys.ToArray();
}
