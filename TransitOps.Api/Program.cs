using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransitOps.Api.Application.Queries.Transports;
using TransitOps.Api.Infrastructure.Queries.Transports;
using TransitOps.Api.Middleware;
using TransitOps.Api.Domain.Enums;
using TransitOps.Api.Infrastructure.Persistence;

namespace TransitOps.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        builder.Services.AddDbContext<TransitOpsDbContext>(
            options => options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MapEnum<TransportStatus>("transport_status");
                    npgsqlOptions.MapEnum<ShipmentEventType>("shipment_event_type");
                    npgsqlOptions.MapEnum<UserRole>("user_role");
                }));

        builder.Services.AddScoped<ITransportQueries, TransportQueries>();

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
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
