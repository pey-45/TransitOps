using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Persistence;
using TransitOps.Api.Security;

namespace TransitOps.Api.Features.Auth;

public sealed class AuthService(
    TransitOpsDbContext dbContext,
    IPasswordHasher<AppUser> passwordHasher,
    JwtOptions jwtOptions,
    BootstrapOptions bootstrapOptions) : IAuthService
{
    public async Task<UserResponse> BootstrapAsync(
        BootstrapAdminRequest request, string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bootstrapOptions.FirstAdminToken))
            throw new ApiException(503, "bootstrap_not_configured", "El arranque del primer administrador no está configurado.");
        if (!string.Equals(token?.Trim(), bootstrapOptions.FirstAdminToken, StringComparison.Ordinal))
            throw new ApiException(401, "invalid_bootstrap_token", "El token de arranque no es válido.");
        if (await dbContext.AppUsers.AnyAsync(user => user.IsActive && user.Role == UserRole.Admin, cancellationToken))
            throw new ApiException(409, "first_admin_already_exists", "Ya existe un administrador activo.");

        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.AppUsers.AnyAsync(user => user.Username == username || user.Email == email, cancellationToken))
            throw new ApiException(409, "user_credentials_conflict", "El nombre de usuario o correo ya está en uso.");

        var user = new AppUser { Username = username, Email = email, PasswordHash = "", Role = UserRole.Admin };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await dbContext.AppUsers.SingleOrDefaultAsync(item => item.Username == username, cancellationToken);
        if (user is null || !user.IsActive ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new ApiException(401, "invalid_credentials", "El usuario o la contraseña no son válidos.");

        var expiresAt = DateTime.UtcNow.AddMinutes(jwtOptions.ExpirationMinutes);
        return new LoginResponse(CreateToken(user, expiresAt), "Bearer", expiresAt, MapUser(user));
    }

    private string CreateToken(AppUser user, DateTime expiresAt)
    {
        var role = user.Role == UserRole.Admin ? RoleNames.Admin : RoleNames.Operator;
        var token = new JwtSecurityToken(
            jwtOptions.Issuer,
            jwtOptions.Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ],
            DateTime.UtcNow,
            expiresAt,
            new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserResponse MapUser(AppUser user) => new(
        user.Id, user.Username, user.Email, user.Role == UserRole.Admin ? RoleNames.Admin : RoleNames.Operator, user.IsActive);
}
