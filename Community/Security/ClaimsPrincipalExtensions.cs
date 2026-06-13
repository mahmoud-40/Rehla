using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BreastCancer.Community.Security;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return  principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
        ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    }

    public static IReadOnlyCollection<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Select(role => role.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
