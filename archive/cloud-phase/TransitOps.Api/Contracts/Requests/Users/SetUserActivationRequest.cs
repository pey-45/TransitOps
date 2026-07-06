namespace TransitOps.Api.Contracts.Requests.Users;

public sealed record SetUserActivationRequest
{
    public bool IsActive { get; init; }
}
