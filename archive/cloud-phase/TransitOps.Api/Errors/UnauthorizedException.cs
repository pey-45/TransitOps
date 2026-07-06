namespace TransitOps.Api.Errors;

public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException(string code, string message)
        : base(StatusCodes.Status401Unauthorized, code, message)
    {
    }
}
