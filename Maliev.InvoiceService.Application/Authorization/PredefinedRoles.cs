namespace Maliev.InvoiceService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Invoice Service.
/// </summary>
public static class InvoicePredefinedRoles
{
    public const string Admin = "roles.invoice.admin";
    public const string Accountant = "roles.invoice.accountant";
    public const string Viewer = "roles.invoice.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Invoice Administrator with full access",
            new[]
            {
                InvoicePermissions.InvoiceCreate,
                InvoicePermissions.InvoiceRead,
                InvoicePermissions.InvoiceUpdate,
                InvoicePermissions.InvoiceDelete,
                InvoicePermissions.InvoiceFinalize,
                InvoicePermissions.InvoiceApprove,
                InvoicePermissions.InvoiceVoid,
                InvoicePermissions.InvoiceExport,
                InvoicePermissions.InvoiceSend,
                InvoicePermissions.SegmentCreate,
                InvoicePermissions.SegmentRead,
                InvoicePermissions.SegmentUpdate,
                InvoicePermissions.SegmentDelete,
                InvoicePermissions.SplitCreate,
                InvoicePermissions.SplitManage,
                InvoicePermissions.FileUpload,
                InvoicePermissions.FileRead,
                InvoicePermissions.FileDelete,
                InvoicePermissions.FileRegister,
                InvoicePermissions.ReportCurrency,
                InvoicePermissions.ReportAnalytics,
                InvoicePermissions.ReportExport,
                InvoicePermissions.BillingNoteCreate,
                InvoicePermissions.BillingNoteRead,
                InvoicePermissions.BillingNoteUpdate,
                InvoicePermissions.BillingNoteDelete,
                InvoicePermissions.CreditTermRead,
                InvoicePermissions.CreditTermManage,
            }
        ),
        (
            Accountant,
            "Invoice Accountant with invoice and report access",
            new[]
            {
                InvoicePermissions.InvoiceCreate,
                InvoicePermissions.InvoiceRead,
                InvoicePermissions.InvoiceUpdate,
                InvoicePermissions.InvoiceFinalize,
                InvoicePermissions.InvoiceApprove,
                InvoicePermissions.InvoiceExport,
                InvoicePermissions.InvoiceSend,
                InvoicePermissions.SegmentCreate,
                InvoicePermissions.SegmentRead,
                InvoicePermissions.SegmentUpdate,
                InvoicePermissions.SplitCreate,
                InvoicePermissions.SplitManage,
                InvoicePermissions.FileUpload,
                InvoicePermissions.FileRead,
                InvoicePermissions.FileRegister,
                InvoicePermissions.ReportCurrency,
                InvoicePermissions.ReportAnalytics,
                InvoicePermissions.ReportExport,
                InvoicePermissions.BillingNoteCreate,
                InvoicePermissions.BillingNoteRead,
                InvoicePermissions.BillingNoteUpdate,
                InvoicePermissions.CreditTermRead,
            }
        ),
        (
            Viewer,
            "Invoice Viewer with read-only access",
            new[]
            {
                InvoicePermissions.InvoiceRead,
                InvoicePermissions.SegmentRead,
                InvoicePermissions.SplitCreate,
                InvoicePermissions.SplitManage,
                InvoicePermissions.FileRead,
                InvoicePermissions.ReportCurrency,
                InvoicePermissions.ReportAnalytics,
                InvoicePermissions.BillingNoteRead,
                InvoicePermissions.CreditTermRead,
            }
        ),
    };
}
