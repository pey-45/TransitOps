using TransitOps.Api.Contracts.Responses.Users;

namespace TransitOps.Api.Contracts.Responses.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAt,
    UserResponse User);
