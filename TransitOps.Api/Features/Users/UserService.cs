using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Auth;
using TransitOps.Api.Persistence;

namespace TransitOps.Api.Features.Users;

public sealed class UserService(
    TransitOpsDbContext dbContext,
    IPasswordHasher<AppUser> passwordHasher) : IUserService
{
    private const long LastAdminLockKey = 2026080701;

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(
        ListUsersQuery query,
        CancellationToken cancellationToken)
    {
        var users = dbContext.AppUsers.AsNoTracking();
        if (query.IncludeInactive is not true)
            users = users.Where(item => item.IsActive);

        return await users
            .OrderBy(item => item.Role)
            .ThenBy(item => item.Username)
            .Select(item => Map(item))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Map(await Existing(id, cancellationToken));

    public async Task<UserResponse> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.AppUsers.AnyAsync(
                item => item.Username == username || item.Email == email,
                cancellationToken))
        {
            throw new ApiException(
                409,
                "user_credentials_conflict",
                "El nombre de usuario o correo ya está en uso.");
        }

        var user = new AppUser
        {
            Username = username,
            Email = email,
            PasswordHash = "",
            Role = UserRoles.Parse(request.Role)
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserResponse> ChangeRoleAsync(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var user = await Existing(id, cancellationToken);
        var role = UserRoles.Parse(request.Role);
        if (user.Role == role)
            return Map(user);

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        if (role == UserRole.Operator)
            await EnsureNotLastAdmin(user, cancellationToken);

        user.Role = role;
        user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(user);
    }

    public async Task<UserResponse> ChangeActivationAsync(
        Guid id,
        UpdateUserActivationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await Existing(id, cancellationToken);
        var isActive = request.IsActive
            ?? throw new ApiException(400, "user_activation_invalid", "El estado indicado no es válido.");
        if (user.IsActive == isActive)
            return Map(user);

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        if (!isActive)
            await EnsureNotLastAdmin(user, cancellationToken);

        user.IsActive = isActive;
        if (!isActive)
            user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return Map(user);
    }

    private async Task<AppUser> Existing(Guid id, CancellationToken cancellationToken) =>
        await dbContext.AppUsers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
        ?? throw new ApiException(404, "user_not_found", "El usuario no existe.");

    private async Task EnsureNotLastAdmin(AppUser user, CancellationToken cancellationToken)
    {
        if (user.Role != UserRole.Admin || !user.IsActive)
            return;

        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({LastAdminLockKey})",
                cancellationToken);
        }

        if (!await dbContext.AppUsers.AnyAsync(
                item => item.Id != user.Id && item.IsActive && item.Role == UserRole.Admin,
                cancellationToken))
        {
            throw new ApiException(
                409,
                "last_admin_protected",
                "No se puede dejar la aplicación sin ningún administrador activo.");
        }
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) =>
        dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

    private static UserResponse Map(AppUser user) => new(
        user.Id,
        user.Username,
        user.Email,
        UserRoles.Token(user.Role),
        user.IsActive);
}
