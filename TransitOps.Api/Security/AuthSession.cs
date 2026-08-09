namespace TransitOps.Api.Security;

public static class AuthSession
{
    public const string CookieName = "transitops_session";
    public const string TokenVersionClaim = "token_version";

    public static CookieOptions CookieOptions(DateTime expiresAt, bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        IsEssential = true,
        Path = "/",
        Expires = expiresAt
    };

    public static CookieOptions DeleteOptions(bool secure) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        IsEssential = true,
        Path = "/"
    };
}
