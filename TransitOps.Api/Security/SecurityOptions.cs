namespace TransitOps.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = "";
    public string Audience { get; init; } = "";
    public string SigningKey { get; init; } = "";
    public int ExpirationMinutes { get; init; } = 60;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience) || SigningKey.Length < 32)
            throw new InvalidOperationException("La configuración JWT no es válida; la clave debe tener al menos 32 caracteres.");
        if (ExpirationMinutes is < 1 or > 1440)
            throw new InvalidOperationException("Jwt:ExpirationMinutes debe estar entre 1 y 1440.");
    }
}

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";
    public string FirstAdminToken { get; init; } = "";
}

public static class RoleNames
{
    public const string Admin = "admin";
    public const string Operator = "operator";
}

public static class Policies
{
    public const string Operational = "operational";
    public const string Admin = "admin";
}
