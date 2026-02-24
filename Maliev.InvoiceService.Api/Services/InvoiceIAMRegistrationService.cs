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
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public InvoiceIAMRegistrationService(
        IConfiguration configuration,
        ILogger<InvoiceIAMRegistrationService> logger)
        : base(configuration, logger, "invoice")
    {
    }

    /// <summary>
    /// Gets the list of permissions to register with IAM.
    /// </summary>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return InvoicePermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <summary>
    /// Gets the list of predefined roles to register with IAM.
    /// </summary>
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return InvoicePredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}
