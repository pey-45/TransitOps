namespace TransitOps.Api.Errors;

public sealed class ServiceUnavailableException : ApiException
{
    public ServiceUnavailableException(string code, string message)
        : base(StatusCodes.Status503ServiceUnavailable, code, message)
    {
    }
}
