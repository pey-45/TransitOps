using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TransitOps.Api.Application.Auth;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Auth;
using TransitOps.Api.Contracts.Responses.Auth;
using TransitOps.Api.Contracts.Responses.Users;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Errors;
using TransitOps.Api.Infrastructure.Persistence;
using TransitOps.Api.Security;

namespace TransitOps.Api.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly TransitOpsDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly JwtOptions _jwtOptions;
    private readonly BootstrapOptions _bootstrapOptions;

    public AuthService(
        TransitOpsDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher,
        JwtOptions jwtOptions,
        BootstrapOptions bootstrapOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtOptions = jwtOptions;
        _bootstrapOptions = bootstrapOptions;
    }

    public async Task<UserResponse> BootstrapFirstAdminAsync(
        BootstrapFirstAdminRequest request,
        string? bootstrapToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_bootstrapOptions.FirstAdminToken))
        {
            throw new ServiceUnavailableException(
                "first_admin_bootstrap_not_configured",
                "First-admin bootstrap is not configured in this environment.");
        }

        if (!string.Equals(
                _bootstrapOptions.FirstAdminToken,
                bootstrapToken?.Trim(),
                StringComparison.Ordinal))
        {
            throw new UnauthorizedException(
                "invalid_bootstrap_token",
                "The provided bootstrap token is invalid.");
        }

        var hasActiveAdmin = await _dbContext.AppUsers
            .AnyAsync(
                appUser => appUser.DeletedAt == null
                    && appUser.IsActive
                    && appUser.UserRole == UserRole.Admin,
                cancellationToken);

        if (hasActiveAdmin)
        {
            throw new ConflictException(
                "first_admin_already_exists",
                "The first admin bootstrap flow is no longer available because an active admin already exists.");
        }

        var username = NormalizeUsername(request.Username);
        var email = NormalizeEmail(request.Email);

        await EnsureUniqueCredentialsAsync(username, email, cancellationToken);

        var appUser = new AppUser
        {
            Username = username,
            Email = email,
            UserRole = UserRole.Admin,
            IsActive = true
        };

        appUser.PasswordHash = _passwordHasher.HashPassword(appUser, request.Password);

        _dbContext.AppUsers.Add(appUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(appUser);
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var username = NormalizeUsername(request.Username);

        var appUser = await _dbContext.AppUsers
            .SingleOrDefaultAsync(
                candidate => candidate.DeletedAt == null
                    && candidate.Username == username,
                cancellationToken);

        if (appUser is null || !appUser.IsActive)
        {
            throw new UnauthorizedException(
                "invalid_credentials",
                "The provided credentials are invalid.");
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            appUser,
            appUser.PasswordHash,
            request.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedException(
                "invalid_credentials",
                "The provided credentials are invalid.");
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            appUser.PasswordHash = _passwordHasher.HashPassword(appUser, request.Password);
            appUser.UpdatedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var accessToken = CreateAccessToken(appUser, expiresAt);

        return new LoginResponse(
            accessToken,
            "Bearer",
            expiresAt,
            MapToResponse(appUser));
    }

    private async Task EnsureUniqueCredentialsAsync(
        string username,
        string email,
        CancellationToken cancellationToken)
    {
        var usernameExists = await _dbContext.AppUsers
            .AnyAsync(
                appUser => appUser.DeletedAt == null
                    && appUser.Username == username,
                cancellationToken);

        if (usernameExists)
        {
            throw new ConflictException(
                "app_user_username_conflict",
                $"Username '{username}' is already used by an active user.");
        }

        var emailExists = await _dbContext.AppUsers
            .AnyAsync(
                appUser => appUser.DeletedAt == null
                    && appUser.Email == email,
                cancellationToken);

        if (emailExists)
        {
            throw new ConflictException(
                "app_user_email_conflict",
                $"Email '{email}' is already used by an active user.");
        }
    }

    private string CreateAccessToken(AppUser appUser, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, appUser.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, appUser.Username),
            new Claim(JwtRegisteredClaimNames.Email, appUser.Email),
            new Claim("role", appUser.UserRole.ToClaimValue()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var securityToken = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }

    private static UserResponse MapToResponse(AppUser appUser)
    {
        return new UserResponse(
            appUser.Id,
            appUser.Username,
            appUser.Email,
            appUser.UserRole,
            appUser.IsActive,
            appUser.CreatedAt,
            appUser.UpdatedAt);
    }

    private static string NormalizeUsername(string value)
    {
        return value.Trim();
    }

    private static string NormalizeEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
