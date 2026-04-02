using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TransitOps.Api.Application.Auth;
using TransitOps.Api.Application.Drivers;
using TransitOps.Api.Application.ShipmentEvents;
using TransitOps.Api.Application.Transports;
using TransitOps.Api.Application.Users;
using TransitOps.Api.Application.Vehicles;
using TransitOps.Api.Common;
using TransitOps.Api.Domain.Entities;
using TransitOps.Api.Middleware;
using TransitOps.Api.Infrastructure.Auth;
using TransitOps.Api.Infrastructure.Drivers;
using TransitOps.Api.Infrastructure.Persistence;
using TransitOps.Api.Infrastructure.ShipmentEvents;
using TransitOps.Api.Infrastructure.Transports;
using TransitOps.Api.Infrastructure.Users;
using TransitOps.Api.Infrastructure.Vehicles;
using TransitOps.Api.Security;

namespace TransitOps.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        builder.Services
            .AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var validationErrors = context.ModelState
                        .Where(modelState => modelState.Value?.Errors.Count > 0)
                        .ToDictionary(
                            keySelector: modelState => modelState.Key,
                            elementSelector: modelState => modelState.Value!.Errors
                                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                    ? "The submitted value is invalid."
                                    : error.ErrorMessage)
                                .ToArray());

                    var response = Common.ApiErrorResponse.Create(
                        code: "validation_error",
                        message: "One or more validation errors occurred.",
                        requestId: context.HttpContext.TraceIdentifier,
                        details: validationErrors);

                    return new BadRequestObjectResult(response);
                };
            });

        builder.Services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        var jwtOptions = builder.Configuration
            .GetRequiredSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? new JwtOptions();
        jwtOptions.Validate();

        var bootstrapOptions = builder.Configuration
            .GetSection(BootstrapOptions.SectionName)
            .Get<BootstrapOptions>()
            ?? new BootstrapOptions();

        builder.Services.AddDbContext<TransitOpsDbContext>(
            options => options.UseNpgsql(connectionString));

        builder.Services.AddSingleton(jwtOptions);
        builder.Services.AddSingleton(bootstrapOptions);
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IDriverService, DriverService>();
        builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        builder.Services.AddScoped<IShipmentEventService, ShipmentEventService>();
        builder.Services.AddScoped<ITransportService, TransportService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IVehicleService, VehicleService>();
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    NameClaimType = "unique_name",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";

                        var payload = ApiErrorResponse.Create(
                            "authentication_required",
                            "A valid bearer token is required to access this resource.",
                            context.HttpContext.TraceIdentifier);

                        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, jsonOptions));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";

                        var payload = ApiErrorResponse.Create(
                            "authorization_forbidden",
                            "You do not have permission to access this resource.",
                            context.HttpContext.TraceIdentifier);

                        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, jsonOptions));
                    }
                };
            });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.OperationalAccess,
                policy => policy.RequireRole(RoleNames.Admin, RoleNames.Operator));
            options.AddPolicy(
                AuthorizationPolicies.AdminAccess,
                policy => policy.RequireRole(RoleNames.Admin));
        });

        builder.Services.AddOpenApi();

        var app = builder.Build();

        var applyMigrationsOnStartup = app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");

        if (applyMigrationsOnStartup)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();

            if (dbContext.Database.IsRelational())
            {
                dbContext.Database.Migrate();
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
