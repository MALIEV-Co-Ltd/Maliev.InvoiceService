using Maliev.Aspire.ServiceDefaults.IAM;

namespace Maliev.InvoiceService.Api.Authorization;

/// <summary>
/// Defines predefined roles for the Invoice Service.
/// Roles follow the GCP format: roles.invoice.{role-name}
/// </summary>
public static class InvoicePredefinedRoles
{
    /// <summary>Invoice Administrator role.</summary>
    public static readonly RoleRegistration Admin = new()
    {
        RoleId = "roles.invoice.admin",
        Description = "Full administrative access to all invoice operations",
        PermissionIds = InvoicePermissions.All.ToList()
    };

    /// <summary>Invoice Manager role.</summary>
    public static readonly RoleRegistration Manager = new()
    {
        RoleId = "roles.invoice.manager",
        Description = "Can create, update, finalize, and approve invoices",
        PermissionIds = new List<string>
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
        }
    };

    /// <summary>Invoice Creator role.</summary>
    public static readonly RoleRegistration Creator = new()
    {
        RoleId = "roles.invoice.creator",
        Description = "Can create and manage own invoices",
        PermissionIds = new List<string>
        {
            InvoicePermissions.InvoicesCreate,
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.InvoicesUpdate,
            InvoicePermissions.SegmentsCreate,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.SegmentsUpdate,
            InvoicePermissions.FilesUpload,
            InvoicePermissions.FilesRead
        }
    };

    /// <summary>Invoice Viewer role.</summary>
    public static readonly RoleRegistration Viewer = new()
    {
        RoleId = "roles.invoice.viewer",
        Description = "Read-only access to invoices",
        PermissionIds = new List<string>
        {
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.FilesRead,
            InvoicePermissions.ReportsCurrency
        }
    };

    /// <summary>Invoice Accountant role.</summary>
    public static readonly RoleRegistration Accountant = new()
    {
        RoleId = "roles.invoice.accountant",
        Description = "Can approve, void, and manage financial aspects",
        PermissionIds = new List<string>
        {
            InvoicePermissions.InvoicesRead,
            InvoicePermissions.InvoicesApprove,
            InvoicePermissions.InvoicesVoid,
            InvoicePermissions.InvoicesFinalize,
            InvoicePermissions.SegmentsRead,
            InvoicePermissions.ReportsCurrency,
            InvoicePermissions.ReportsAnalytics,
            InvoicePermissions.ReportsExport
        }
    };

    /// <summary>All predefined roles.</summary>
    public static readonly RoleRegistration[] All = new[]
    {
        Admin,
        Manager,
        Creator,
        Viewer,
        Accountant
    };
}