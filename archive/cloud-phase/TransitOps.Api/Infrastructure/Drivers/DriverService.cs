using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Application.Drivers;
using TransitOps.Api.Common;
using TransitOps.Api.Contracts.Requests.Drivers;
using TransitOps.Api.Contracts.Responses.Drivers;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Errors;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Api.Infrastructure.Drivers;

public sealed class DriverService : IDriverService
{
    private readonly TransitOpsDbContext _dbContext;

    public DriverService(TransitOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DriverResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Drivers
            .AsNoTracking()
            .Where(driver => driver.DeletedAt == null)
            .OrderBy(driver => driver.LastName)
            .ThenBy(driver => driver.FirstName)
            .ThenBy(driver => driver.LicenseNumber)
            .Select(driver => new DriverResponse(
                driver.Id,
                driver.EmployeeCode,
                driver.FirstName,
                driver.LastName,
                driver.LicenseNumber,
                driver.LicenseExpiryDate,
                driver.Phone,
                driver.Email,
                driver.IsActive,
                driver.CreatedAt,
                driver.UpdatedAt,
                driver.DeletedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<DriverResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.Drivers
            .AsNoTracking()
            .Where(driver => driver.Id == id && driver.DeletedAt == null)
            .Select(driver => new DriverResponse(
                driver.Id,
                driver.EmployeeCode,
                driver.FirstName,
                driver.LastName,
                driver.LicenseNumber,
                driver.LicenseExpiryDate,
                driver.Phone,
                driver.Email,
                driver.IsActive,
                driver.CreatedAt,
                driver.UpdatedAt,
                driver.DeletedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<DriverResponse> CreateAsync(
        UpsertDriverRequest request,
        CancellationToken cancellationToken)
    {
        var licenseNumber = request.LicenseNumber.Trim();
        var employeeCode = NormalizeOptionalText(request.EmployeeCode);
        var email = NormalizeOptionalText(request.Email);

        await EnsureLicenseNumberIsUniqueAsync(licenseNumber, excludedDriverId: null, cancellationToken);
        await EnsureEmployeeCodeIsUniqueAsync(employeeCode, excludedDriverId: null, cancellationToken);
        await EnsureEmailIsUniqueAsync(email, excludedDriverId: null, cancellationToken);

        var driver = new Driver
        {
            EmployeeCode = employeeCode,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            LicenseNumber = licenseNumber,
            LicenseExpiryDate = request.LicenseExpiryDate,
            Phone = NormalizeOptionalText(request.Phone),
            Email = email,
            IsActive = request.IsActive
        };

        _dbContext.Drivers.Add(driver);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(driver);
    }

    public async Task<DriverResponse> UpdateAsync(
        Guid id,
        UpsertDriverRequest request,
        CancellationToken cancellationToken)
    {
        var driver = await GetActiveDriverAsync(id, cancellationToken);
        var licenseNumber = request.LicenseNumber.Trim();
        var employeeCode = NormalizeOptionalText(request.EmployeeCode);
        var email = NormalizeOptionalText(request.Email);

        await EnsureLicenseNumberIsUniqueAsync(licenseNumber, driver.Id, cancellationToken);
        await EnsureEmployeeCodeIsUniqueAsync(employeeCode, driver.Id, cancellationToken);
        await EnsureEmailIsUniqueAsync(email, driver.Id, cancellationToken);

        driver.EmployeeCode = employeeCode;
        driver.FirstName = request.FirstName.Trim();
        driver.LastName = request.LastName.Trim();
        driver.LicenseNumber = licenseNumber;
        driver.LicenseExpiryDate = request.LicenseExpiryDate;
        driver.Phone = NormalizeOptionalText(request.Phone);
        driver.Email = email;
        driver.IsActive = request.IsActive;
        driver.UpdatedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToResponse(driver);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var driver = await GetActiveDriverAsync(id, cancellationToken);
        var deletedAt = DateTimePersistence.AsUnspecified(DateTime.UtcNow);

        driver.DeletedAt = deletedAt;
        driver.UpdatedAt = deletedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureLicenseNumberIsUniqueAsync(
        string licenseNumber,
        Guid? excludedDriverId,
        CancellationToken cancellationToken)
    {
        var existingDriverQuery = _dbContext.Drivers
            .Where(driver => driver.DeletedAt == null && driver.LicenseNumber == licenseNumber);

        if (excludedDriverId.HasValue)
        {
            existingDriverQuery = existingDriverQuery
                .Where(driver => driver.Id != excludedDriverId.Value);
        }

        if (await existingDriverQuery.AnyAsync(cancellationToken))
        {
            throw new ConflictException(
                "driver_license_number_conflict",
                $"Driver license number '{licenseNumber}' already exists.");
        }
    }

    private async Task EnsureEmployeeCodeIsUniqueAsync(
        string? employeeCode,
        Guid? excludedDriverId,
        CancellationToken cancellationToken)
    {
        if (employeeCode is null)
        {
            return;
        }

        var existingDriverQuery = _dbContext.Drivers
            .Where(driver => driver.DeletedAt == null && driver.EmployeeCode == employeeCode);

        if (excludedDriverId.HasValue)
        {
            existingDriverQuery = existingDriverQuery
                .Where(driver => driver.Id != excludedDriverId.Value);
        }

        if (await existingDriverQuery.AnyAsync(cancellationToken))
        {
            throw new ConflictException(
                "driver_employee_code_conflict",
                $"Driver employee code '{employeeCode}' already exists.");
        }
    }

    private async Task EnsureEmailIsUniqueAsync(
        string? email,
        Guid? excludedDriverId,
        CancellationToken cancellationToken)
    {
        if (email is null)
        {
            return;
        }

        var existingDriverQuery = _dbContext.Drivers
            .Where(driver => driver.DeletedAt == null && driver.Email == email);

        if (excludedDriverId.HasValue)
        {
            existingDriverQuery = existingDriverQuery
                .Where(driver => driver.Id != excludedDriverId.Value);
        }

        if (await existingDriverQuery.AnyAsync(cancellationToken))
        {
            throw new ConflictException(
                "driver_email_conflict",
                $"Driver email '{email}' already exists.");
        }
    }

    private async Task<Driver> GetActiveDriverAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var driver = await _dbContext.Drivers
            .SingleOrDefaultAsync(
                existingDriver => existingDriver.Id == id && existingDriver.DeletedAt == null,
                cancellationToken);

        if (driver is null)
        {
            throw new ResourceNotFoundException("driver_not_found", $"Driver '{id}' was not found.");
        }

        return driver;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static DriverResponse MapToResponse(Driver driver)
    {
        return new DriverResponse(
            driver.Id,
            driver.EmployeeCode,
            driver.FirstName,
            driver.LastName,
            driver.LicenseNumber,
            driver.LicenseExpiryDate,
            driver.Phone,
            driver.Email,
            driver.IsActive,
            driver.CreatedAt,
            driver.UpdatedAt,
            driver.DeletedAt);
    }
}
