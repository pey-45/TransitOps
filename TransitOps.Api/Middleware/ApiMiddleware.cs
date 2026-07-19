using TransitOps.Api.Common;

namespace TransitOps.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        const string header = "X-Correlation-ID";
        var id = context.Request.Headers.TryGetValue(header, out var supplied) && !string.IsNullOrWhiteSpace(supplied)
            ? supplied.ToString() : Guid.NewGuid().ToString("N");
        context.TraceIdentifier = id;
        context.Response.Headers[header] = id;
        await next(context);
    }
}

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ApiException exception)
        {
            context.Response.StatusCode = exception.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(exception.Code, exception.Message, context.TraceIdentifier));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API error for request {RequestId}", context.TraceIdentifier);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(ApiErrorResponse.Create(
                "internal_error", "Se ha producido un error inesperado.", context.TraceIdentifier));
        }
    }
}
