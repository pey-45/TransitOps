using TransitOps.Api.Domain.Enums;

namespace TransitOps.Api.Contracts.Responses.Users;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string Email,
    UserRole UserRole,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
