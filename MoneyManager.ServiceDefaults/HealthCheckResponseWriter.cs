using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.Hosting;

internal static class HealthCheckResponseWriter
{
	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

	/// <summary>
	/// Writes a minimal health payload without exception details or internal data.
	/// </summary>
	public static Task WriteMinimalResponse(HttpContext context, HealthReport report)
	{
		context.Response.ContentType = "application/json; charset=utf-8";

		var payload = new
		{
			status = report.Status.ToString(),
			totalDuration = report.TotalDuration.TotalMilliseconds,
			entries = report.Entries.ToDictionary(
				entry => entry.Key,
				entry => new
				{
					status = entry.Value.Status.ToString(),
					description = entry.Value.Description,
					duration = entry.Value.Duration.TotalMilliseconds,
				}),
		};

		return context.Response.WriteAsJsonAsync(payload, JsonOptions);
	}

	public static HealthCheckOptions WithMinimalResponse(this HealthCheckOptions options)
	{
		options.ResponseWriter = WriteMinimalResponse;
		return options;
	}
}
