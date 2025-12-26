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

    /// <summary>
    /// All permissions defined for the Invoice Service.
    /// </summary>
    public static readonly string[] All = new[]
    {
        InvoicesCreate, InvoicesRead, InvoicesUpdate, InvoicesDelete, InvoicesFinalize, InvoicesApprove, InvoicesVoid, InvoicesExport, InvoicesSend,
        SegmentsCreate, SegmentsRead, SegmentsUpdate, SegmentsDelete,
        SplitsCreate, SplitsManage,
        FilesUpload, FilesRead, FilesDelete, FilesRegister,
        ReportsCurrency, ReportsAnalytics, ReportsExport
    };
}