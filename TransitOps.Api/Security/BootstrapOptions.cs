namespace TransitOps.Api.Security;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    public string FirstAdminToken { get; init; } = string.Empty;
}
