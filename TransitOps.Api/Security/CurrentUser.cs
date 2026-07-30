namespace TransitOps.Api.Security;

public interface ICurrentUser
{
    Guid? Id { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? Id => Guid.TryParse(
        accessor.HttpContext?.User.FindFirst("sub")?.Value,
        out var id)
        ? id
        : null;
}
