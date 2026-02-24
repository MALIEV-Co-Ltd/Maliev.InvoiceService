using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Maliev.InvoiceService.Api.Authorization;

/// <summary>
/// Transforms principal claims by mapping broad roles to fine-grained permissions if permissions are missing.
/// </summary>
public class IAMClaimsTransformation : IClaimsTransformation
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<IAMClaimsTransformation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IAMClaimsTransformation"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public IAMClaimsTransformation(IConfiguration configuration, ILogger<IAMClaimsTransformation> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Transforms the specified principal.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <returns>The transformed principal.</returns>
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var iamEnabled = _configuration.GetValue<bool>("Features:PermissionBasedAuthEnabled");
        if (!iamEnabled)
        {
            return Task.FromResult(principal);
        }

        // If permissions are already present, follow precedence rule (permissions > roles)
        if (principal.HasClaim(c => c.Type == "permissions"))
        {
            return Task.FromResult(principal);
        }

        // Mapping legacy roles to permissions
        var clone = principal.Clone();
        var newIdentity = clone.Identity as ClaimsIdentity;
        if (newIdentity == null) return Task.FromResult(principal);

        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var mappedPermissions = new HashSet<string>();

        foreach (var role in roles)
        {
            var normalizedRole = role.ToLower();
            var fullRoleId = normalizedRole.StartsWith("roles.invoice.") ? normalizedRole : $"roles.invoice.{normalizedRole}";
            var roleTuple = InvoicePredefinedRoles.All.FirstOrDefault(r => r.RoleId == fullRoleId);

            if (roleTuple.RoleId != null)
            {
                foreach (var p in roleTuple.Permissions) mappedPermissions.Add(p);
            }
        }

        foreach (var permission in mappedPermissions)
        {
            newIdentity.AddClaim(new Claim("permissions", permission));
        }

        if (mappedPermissions.Any())
        {
            _logger.LogInformation("Mapped legacy roles [{Roles}] to {Count} permissions",
                string.Join(", ", roles), mappedPermissions.Count);
        }

        return Task.FromResult(clone);
    }
}
