using Microsoft.AspNetCore.Mvc;
using TransitOps.Api.Middleware;

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

        builder.Services.AddOpenApi();

        var app = builder.Build();

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
