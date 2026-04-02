using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Requests.Users;

internal static class UserRoleRequestParser
{
    public static bool TryParse(string value, out UserRole userRole)
    {
        var normalized = value.Trim()
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        switch (normalized)
        {
            case "admin":
                userRole = UserRole.Admin;
                return true;
            case "operator":
                userRole = UserRole.Operator;
                return true;
            default:
                userRole = default;
                return false;
        }
    }
}
