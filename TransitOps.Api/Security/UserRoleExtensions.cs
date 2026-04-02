using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Security;

public static class UserRoleExtensions
{
    public static string ToClaimValue(this UserRole role)
    {
        return role switch
        {
            UserRole.Admin => RoleNames.Admin,
            UserRole.Operator => RoleNames.Operator,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported user role.")
        };
    }
}
