using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MoneyManager.API.Health;

/// <summary>
/// Verifies SQL Server connectivity. Intended for explicit /health/db probes — not liveness/readiness by default —
/// so auto-pause databases are not kept awake by frequent platform health checks.
/// </summary>
public sealed class SqlConnectionHealthCheck(
	IConfiguration configuration,
	ILogger<SqlConnectionHealthCheck> logger) : IHealthCheck
{
	private const int DefaultCommandTimeoutSeconds = 90;

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		var connectionString = configuration.GetConnectionString("DefaultConnection");
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return HealthCheckResult.Unhealthy("Connection string 'DefaultConnection' is not configured.");
		}

		var commandTimeout = configuration.GetValue(
			"HealthChecks:CommandTimeoutSeconds",
			DefaultCommandTimeoutSeconds);

		try
		{
			await using var connection = new SqlConnection(connectionString);
			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

			await using (var pingCommand = connection.CreateCommand())
			{
				pingCommand.CommandText = "SELECT 1";
				pingCommand.CommandTimeout = commandTimeout;
				var pingResult = await pingCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
				if (pingResult is null || Convert.ToInt32(pingResult) != 1)
				{
					return HealthCheckResult.Unhealthy("SQL ping did not return the expected value.");
				}
			}

			// Warm a real table read — SELECT 1 alone can succeed before the server accepts app queries.
			await using (var dataCommand = connection.CreateCommand())
			{
				dataCommand.CommandText = "SELECT TOP 1 Category_I FROM Categories";
				dataCommand.CommandTimeout = commandTimeout;
				await dataCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
			}

			return HealthCheckResult.Healthy("SQL Server connection and read query succeeded.");
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "SQL Server health check failed.");
			return HealthCheckResult.Unhealthy("SQL Server connection failed.");
		}
	}
}
