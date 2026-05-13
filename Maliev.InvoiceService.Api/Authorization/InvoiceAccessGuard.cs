using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Maliev.InvoiceService.Api.Authorization;

/// <summary>
/// Applies invoice object-scope rules that are narrower than endpoint action permissions.
/// </summary>
public sealed class InvoiceAccessGuard
{
    private static readonly string[] RoleClaimTypes =
    [
        ClaimTypes.Role,
        "role",
        "roles",
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    ];

    private readonly InvoiceDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceAccessGuard"/> class.
    /// </summary>
    /// <param name="context">Invoice database context.</param>
    public InvoiceAccessGuard(InvoiceDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Builds the invoice visibility scope for the current principal.
    /// </summary>
    /// <param name="user">Authenticated principal.</param>
    /// <returns>Invoice access scope.</returns>
    public InvoiceAccessScope GetScope(ClaimsPrincipal user)
    {
        var principalId = GetPrincipalId(user);
        if (string.IsNullOrWhiteSpace(principalId))
        {
            return new InvoiceAccessScope(string.Empty, RestrictToCreatedInvoices: true);
        }

        var roles = GetRoles(user);
        var hasWildcardPermission = user.Claims.Any(c =>
            (c.Type == "permissions" || c.Type == "permission") &&
            string.Equals(c.Value, "*", StringComparison.OrdinalIgnoreCase));

        var hasCreatorRole = roles.Any(IsCreatorRole);
        var hasUnrestrictedRole = roles.Any(IsUnrestrictedRole) || hasWildcardPermission;

        return new InvoiceAccessScope(principalId, hasCreatorRole && !hasUnrestrictedRole);
    }

    /// <summary>
    /// Checks whether the current principal can access an invoice by id.
    /// </summary>
    /// <param name="invoiceId">Invoice identifier.</param>
    /// <param name="user">Authenticated principal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access decision.</returns>
    public async Task<InvoiceAccessDecision> CheckInvoiceAsync(
        Guid invoiceId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var scope = GetScope(user);
        if (!scope.RestrictToCreatedInvoices)
        {
            return InvoiceAccessDecision.Allowed;
        }

        if (string.IsNullOrWhiteSpace(scope.PrincipalId))
        {
            return InvoiceAccessDecision.Forbidden;
        }

        var access = await _context.Invoices
            .Where(i => i.Id == invoiceId && !i.IsDeleted)
            .Select(i => new
            {
                CanAccess = i.AuditLogs.Any(a =>
                    a.EventType == "Created" &&
                    a.ActorId == scope.PrincipalId)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (access == null)
        {
            return InvoiceAccessDecision.NotFound;
        }

        return access.CanAccess ? InvoiceAccessDecision.Allowed : InvoiceAccessDecision.Forbidden;
    }

    private static string? GetPrincipalId(ClaimsPrincipal user)
    {
        return user.FindFirst("user_id")?.Value
            ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static HashSet<string> GetRoles(ClaimsPrincipal user)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in user.Claims.Where(c => RoleClaimTypes.Contains(c.Type)))
        {
            foreach (var value in claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                roles.Add(value);
            }
        }

        return roles;
    }

    private static bool IsCreatorRole(string role)
    {
        return string.Equals(role, "creator", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, InvoicePredefinedRoles.Creator, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnrestrictedRole(string role)
    {
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, InvoicePredefinedRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "manager", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, InvoicePredefinedRoles.Manager, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "accountant", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, InvoicePredefinedRoles.Accountant, StringComparison.OrdinalIgnoreCase);
    }
}
