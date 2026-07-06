using System.Diagnostics;
using System.Security.Claims;

namespace TransitOps.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    private const int MaxCorrelationIdLength = 128;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.TraceIdentifier = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["correlationId"] = correlationId
        });

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP {HttpMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds} ms with correlation {CorrelationId} for user {UserId} with role {UserRole}.",
                context.Request.Method,
                context.Request.Path.Value ?? string.Empty,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId,
                context.User.FindFirstValue("sub") ?? "anonymous",
                context.User.FindFirstValue("role") ?? "anonymous");
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var submittedValue = values.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(submittedValue))
            {
                return submittedValue.Trim().Length <= MaxCorrelationIdLength
                    ? submittedValue.Trim()
                    : submittedValue.Trim()[..MaxCorrelationIdLength];
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
