using System.Diagnostics;
using MoneyManager.API.Utilities;

namespace MoneyManager.API.Middleware;

/// <summary>
/// Logs each HTTP request with method, path, status, duration, and resolved user id.
/// </summary>
public sealed class RequestLoggingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<RequestLoggingMiddleware> _logger;

	public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context, IResolveUserId resolveUserId)
	{
		if (IsHealthCheck(context.Request.Path))
		{
			await _next(context);
			return;
		}

		var stopwatch = Stopwatch.StartNew();
		try
		{
			await _next(context);
		}
		finally
		{
			stopwatch.Stop();
			var userId = resolveUserId.Resolve(context.User);
			var logLevel = context.Response.StatusCode >= 500 ? LogLevel.Error
				: context.Response.StatusCode >= 400 ? LogLevel.Warning
				: LogLevel.Information;

			_logger.Log(
				logLevel,
				"HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms for user {UserId}",
				context.Request.Method,
				context.Request.Path.Value,
				context.Response.StatusCode,
				stopwatch.ElapsedMilliseconds,
				userId?.ToString() ?? "anonymous");
		}
	}

	private static bool IsHealthCheck(PathString path) =>
		path.StartsWithSegments("/health") || path.StartsWithSegments("/alive");
}
