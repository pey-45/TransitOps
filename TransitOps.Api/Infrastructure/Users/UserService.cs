using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Application.Users;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Users;
using TransitOps.Api.Contracts.Responses.Users;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Errors;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Api.Infrastructure.Users;

public sealed class UserService : IUserService
{
    private readonly TransitOpsDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public UserService(
        TransitOpsDbContext dbContext,
        IPasswordHasher<AppUser> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.AppUsers
            .AsNoTracking()
            .Where(appUser => appUser.DeletedAt == null)
            .OrderBy(appUser => appUser.Username)
            .ThenBy(appUser => appUser.Email)
            .Select(appUser => new UserResponse(
                appUser.Id,
                appUser.Username,
                appUser.Email,
                appUser.UserRole,
                appUser.IsActive,
                appUser.CreatedAt,
                appUser.UpdatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.AppUsers
            .AsNoTracking()
            .Where(appUser => appUser.Id == id && appUser.DeletedAt == null)
            .Select(appUser => new UserResponse(
                appUser.Id,
                appUser.Username,
                appUser.Email,
                appUser.UserRole,
                appUser.IsActive,
                appUser.CreatedAt,
                appUser.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var username = NormalizeUsername(request.Username);
        var email = NormalizeEmail(request.Email);
        var userRole = request.ParseUserRole();

        await EnsureUniqueCredentialsAsync(username, email, excludedUserId: null, cancellationToken);

        var appUser = new AppUser
        {
            Username = username,
            Email = email,
            UserRole = userRole,
            IsActive = true
        };

        appUser.PasswordHash = _passwordHasher.HashPassword(appUser, request.Password);

        _dbContext.AppUsers.Add(appUser);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(appUser);
    }

    public async Task<UserResponse> ChangeRoleAsync(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var appUser = await GetManagedUserAsync(id, cancellationToken);
        var targetRole = request.ParseUserRole();

        if (appUser.UserRole == targetRole)
        {
            return MapToResponse(appUser);
        }

        if (targetRole != UserRole.Admin)
        {
            await EnsureCanLoseActiveAdminStatusAsync(
                appUser,
                "last_active_admin_role_change_forbidden",
                "The last active admin user cannot lose the admin role.",
                cancellationToken);
        }

        appUser.UserRole = targetRole;
        appUser.UpdatedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(appUser);
    }

    public async Task<UserResponse> SetActivationAsync(
        Guid id,
        SetUserActivationRequest request,
        CancellationToken cancellationToken)
    {
        var appUser = await GetManagedUserAsync(id, cancellationToken);

        if (appUser.IsActive == request.IsActive)
        {
            return MapToResponse(appUser);
        }

        if (!request.IsActive)
        {
            await EnsureCanLoseActiveAdminStatusAsync(
                appUser,
                "last_active_admin_deactivation_forbidden",
                "The last active admin user cannot be deactivated.",
                cancellationToken);
        }

        appUser.IsActive = request.IsActive;
        appUser.UpdatedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(appUser);
    }

    private async Task EnsureUniqueCredentialsAsync(
        string username,
        string email,
        Guid? excludedUserId,
        CancellationToken cancellationToken)
    {
        var usernameQuery = _dbContext.AppUsers
            .Where(appUser => appUser.DeletedAt == null && appUser.Username == username);

        var emailQuery = _dbContext.AppUsers
            .Where(appUser => appUser.DeletedAt == null && appUser.Email == email);

        if (excludedUserId.HasValue)
        {
            usernameQuery = usernameQuery.Where(appUser => appUser.Id != excludedUserId.Value);
            emailQuery = emailQuery.Where(appUser => appUser.Id != excludedUserId.Value);
        }

        if (await usernameQuery.AnyAsync(cancellationToken))
        {
            throw new ConflictException(
                "app_user_username_conflict",
                $"Username '{username}' is already used by an active user.");
        }

        if (await emailQuery.AnyAsync(cancellationToken))
        {
            throw new ConflictException(
                "app_user_email_conflict",
                $"Email '{email}' is already used by an active user.");
        }
    }

    private async Task EnsureCanLoseActiveAdminStatusAsync(
        AppUser appUser,
        string conflictCode,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        if (appUser.UserRole != UserRole.Admin || !appUser.IsActive)
        {
            return;
        }

        var otherActiveAdminExists = await _dbContext.AppUsers
            .AnyAsync(
                candidate => candidate.Id != appUser.Id
                    && candidate.DeletedAt == null
                    && candidate.IsActive
                    && candidate.UserRole == UserRole.Admin,
                cancellationToken);

        if (!otherActiveAdminExists)
        {
            throw new ConflictException(conflictCode, conflictMessage);
        }
    }

    private async Task<AppUser> GetManagedUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var appUser = await _dbContext.AppUsers
            .SingleOrDefaultAsync(
                existingUser => existingUser.Id == id && existingUser.DeletedAt == null,
                cancellationToken);

        if (appUser is null)
        {
            throw new ResourceNotFoundException("app_user_not_found", $"User '{id}' was not found.");
        }

        return appUser;
    }

    private static string NormalizeUsername(string value)
    {
        return value.Trim();
    }

    private static string NormalizeEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
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
}
