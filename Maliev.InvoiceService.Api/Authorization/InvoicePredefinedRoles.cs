namespace Maliev.InvoiceService.Api.Authorization;

/// <summary>
/// Predefined roles for the Invoice Service.
/// Roles follow the GCP format: roles.invoice.{role-name}
/// </summary>
public static class InvoicePredefinedRoles
{
    /// <summary>Role for administrators with full access.</summary>
    public const string Admin = "roles.invoice.admin";
    /// <summary>Role for managers who can finalize and approve.</summary>
    public const string Manager = "roles.invoice.manager";
    /// <summary>Role for creators who can manage own invoices.</summary>
    public const string Creator = "roles.invoice.creator";
    /// <summary>Role for users with read-only access.</summary>
    public const string Viewer = "roles.invoice.viewer";
    /// <summary>Role for accountants managing financial aspects.</summary>
    public const string Accountant = "roles.invoice.accountant";

    /// <summary>
    /// Collection of all predefined roles for the Invoice Service.
    /// </summary>
    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (Admin, "Full administrative access to all invoice operations", InvoicePermissions.All),

        (Manager, "Can create, update, finalize, and approve invoices", new[]
        {
            InvoicePermissions.InvoicesCreate,
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.InvoicesUpdate,
            InvoicePermissions.InvoicesFinalize,
            InvoicePermissions.InvoicesApprove,
            InvoicePermissions.InvoicesSend,
            InvoicePermissions.SegmentsCreate,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.SegmentsUpdate,
            InvoicePermissions.FilesUpload,
            InvoicePermissions.FilesRead,
            InvoicePermissions.ReportsCurrency,
            InvoicePermissions.ReportsAnalytics
        }),

        (Creator, "Can create and manage own invoices", new[]
        {
            InvoicePermissions.InvoicesCreate,
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.InvoicesUpdate,
            InvoicePermissions.SegmentsCreate,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.SegmentsUpdate,
            InvoicePermissions.FilesUpload,
            InvoicePermissions.FilesRead
        }),

        (Viewer, "Read-only access to invoices", new[]
        {
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.FilesRead,
            InvoicePermissions.ReportsCurrency
        }),

        (Accountant, "Can approve, void, and manage financial aspects", new[]
        {
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.InvoicesApprove,
            InvoicePermissions.InvoicesVoid,
            InvoicePermissions.InvoicesFinalize,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.ReportsCurrency,
            InvoicePermissions.ReportsAnalytics,
            InvoicePermissions.ReportsExport
        })
    };
}
