namespace TransitOps.Api.Errors;

public sealed class ResourceNotFoundException : ApiException
{
    public ResourceNotFoundException(string code, string message)
        : base(StatusCodes.Status404NotFound, code, message)
    {
    }
}
