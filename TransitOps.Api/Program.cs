using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TransitOps.Api.Common;
using TransitOps.Api.Domain;
using TransitOps.Api.Features.Auth;
using TransitOps.Api.Features.Customers;
using TransitOps.Api.Features.Drivers;
using TransitOps.Api.Features.Reporting;
using TransitOps.Api.Features.Shipments;
using TransitOps.Api.Features.Users;
using TransitOps.Api.Features.Vehicles;
using TransitOps.Api.Middleware;
using TransitOps.Api.Persistence;
using TransitOps.Api.Security;

namespace TransitOps.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var migrateOnly = args.Contains("--migrate-only", StringComparer.OrdinalIgnoreCase);
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(error =>
                            string.IsNullOrWhiteSpace(error.ErrorMessage) ? "El valor indicado no es válido." : error.ErrorMessage).ToArray());

                return new BadRequestObjectResult(ApiErrorResponse.Create(
                    "validation_error",
                    "Hay uno o más datos incorrectos.",
                    context.HttpContext.TraceIdentifier,
                    details));
            };
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection.");
        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new();
        jwtOptions.Validate();
        var bootstrapOptions = builder.Configuration.GetSection(BootstrapOptions.SectionName).Get<BootstrapOptions>() ?? new();

        builder.Services.AddDbContext<TransitOpsDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddSingleton(jwtOptions);
        builder.Services.AddSingleton(bootstrapOptions);
        builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IVehicleService, VehicleService>();
        builder.Services.AddScoped<IDriverService, DriverService>();
        builder.Services.AddScoped<ICustomerService, CustomerService>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        builder.Services.AddScoped<IShipmentService, ShipmentService>();
        builder.Services.AddScoped<IShipmentEventService, ShipmentEventService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<ISummaryService, SummaryService>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                NameClaimType = "unique_name",
                RoleClaimType = "role",
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    if (context.Request.Cookies.TryGetValue(AuthSession.CookieName, out var token) &&
                        !string.IsNullOrWhiteSpace(token))
                    {
                        context.Token = token;
                    }
                    else
                    {
                        context.NoResult();
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (!Guid.TryParse(principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId) ||
                        !int.TryParse(
                            principal?.FindFirst(AuthSession.TokenVersionClaim)?.Value,
                            out var tokenVersion))
                    {
                        context.Fail("La sesión no contiene los claims requeridos.");
                        return;
                    }

                    var database = context.HttpContext.RequestServices.GetRequiredService<TransitOpsDbContext>();
                    var currentVersion = await database.AppUsers.AsNoTracking()
                        .Where(user => user.Id == userId && user.IsActive)
                        .Select(user => (int?)user.TokenVersion)
                        .SingleOrDefaultAsync(context.HttpContext.RequestAborted);
                    if (currentVersion != tokenVersion)
                        context.Fail("La sesión ha sido invalidada.");
                },
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    return WriteAuthError(context.HttpContext, 401, "authentication_required", "Es necesario iniciar sesión.");
                },
                OnForbidden = context => WriteAuthError(context.HttpContext, 403, "authorization_forbidden", "No tienes permiso para realizar esta acción.")
            };
        });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Operational, policy => policy.RequireRole(RoleNames.Admin, RoleNames.Operator))
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(RoleNames.Admin));

        var app = builder.Build();
        if (migrateOnly || app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
        {
            using var scope = app.Services.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<TransitOpsDbContext>();
            if (database.Database.IsRelational()) database.Database.Migrate();
            if (migrateOnly) return;
        }

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }

    private static async Task WriteAuthError(HttpContext context, int status, string code, string message)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(
            ApiErrorResponse.Create(code, message, context.TraceIdentifier),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
