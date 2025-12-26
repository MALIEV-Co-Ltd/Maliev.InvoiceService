using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.InvoiceService.Api.Authorization;

namespace Maliev.InvoiceService.Api.Services;

/// <summary>
/// Service that registers Invoice Service permissions and roles with the IAM service on startup.
/// Fails fast if registration fails to ensure secure state.
/// </summary>
public class InvoiceIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceIAMRegistrationService"/> class.
    /// </summary>
    public InvoiceIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<InvoiceIAMRegistrationService> logger)
        : base(httpClientFactory, logger, "invoice")
    {
    }

    /// <summary>
    /// Gets the list of permissions to register with IAM.
    /// </summary>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return new[]
        {
            // Invoice Operations
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesCreate, Description = "Create new invoices" },
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesRead, Description = "Read invoice details" },
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesUpdate, Description = "Update invoice information" },
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesDelete, Description = "Delete invoices" },
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesFinalize, Description = "Finalize invoices (lock for editing)" },
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesApprove, Description = "Approve invoices" },
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesVoid, Description = "Void/cancel invoices" },
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesExport, Description = "Export invoices to various formats" },
            new PermissionRegistration { PermissionId = InvoicePermissions.InvoicesSend, Description = "Send invoices to customers" },

            // Segment Operations
            new PermissionRegistration { PermissionId = InvoicePermissions.SegmentsCreate, Description = "Create invoice segments" },
            new PermissionRegistration { PermissionId = InvoicePermissions.SegmentsRead, Description = "Read segment details" },
            new PermissionRegistration { PermissionId = InvoicePermissions.SegmentsUpdate, Description = "Update segments" },
            new PermissionRegistration { PermissionId = InvoicePermissions.SegmentsDelete, Description = "Delete segments" },

            // Split Operations
            new PermissionRegistration { PermissionId = InvoicePermissions.SplitsCreate, Description = "Create split invoices" },
            new PermissionRegistration { PermissionId = InvoicePermissions.SplitsManage, Description = "Manage split invoice relationships" },

            // File Operations
            new PermissionRegistration { PermissionId = InvoicePermissions.FilesUpload, Description = "Upload files to invoices" },
            new PermissionRegistration { PermissionId = InvoicePermissions.FilesRead, Description = "Read/download invoice files" },
            new PermissionRegistration { PermissionId = InvoicePermissions.FilesDelete, Description = "Delete invoice files" },
            new PermissionRegistration { PermissionId = InvoicePermissions.FilesRegister, Description = "Register PDF files (service-to-service)" },

            // Reporting Operations
            new PermissionRegistration { PermissionId = InvoicePermissions.ReportsCurrency, Description = "View currency exchange reports" },
            new PermissionRegistration { PermissionId = InvoicePermissions.ReportsAnalytics, Description = "Access invoice analytics" },
            new PermissionRegistration { PermissionId = InvoicePermissions.ReportsExport, Description = "Export reports" }
        };
    }

    /// <summary>
    /// Gets the list of predefined roles to register with IAM.
    /// </summary>
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return InvoicePredefinedRoles.All;
    }
}