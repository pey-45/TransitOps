namespace TransitOps.Api.Errors;

public sealed class ConflictException : ApiException
{
    public ConflictException(string code, string message)
        : base(StatusCodes.Status409Conflict, code, message)
    {
    }
}
