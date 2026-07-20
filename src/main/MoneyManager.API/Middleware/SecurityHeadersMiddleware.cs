namespace MoneyManager.API.Middleware;

/// <summary>
/// Adds baseline security headers to every response.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
	public async Task InvokeAsync(HttpContext context)
	{
		var headers = context.Response.Headers;
		headers.XContentTypeOptions = "nosniff";
		headers.XFrameOptions = "DENY";
		headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
		headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

		await next(context);
	}
}
