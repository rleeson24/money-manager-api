using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MoneyManager.API.Health;

/// <summary>
/// Verifies SQL Server connectivity. Intended for explicit /health/db probes — not liveness/readiness by default —
/// so auto-pause databases are not kept awake by frequent platform health checks.
/// </summary>
public sealed class SqlConnectionHealthCheck(IConfiguration configuration) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		var connectionString = configuration.GetConnectionString("DefaultConnection");
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return HealthCheckResult.Unhealthy("Connection string 'DefaultConnection' is not configured.");
		}

		try
		{
			await using var connection = new SqlConnection(connectionString);
			await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

			await using var command = connection.CreateCommand();
			command.CommandText = "SELECT 1";
			await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

			return HealthCheckResult.Healthy("SQL Server connection succeeded.");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("SQL Server connection failed.", ex);
		}
	}
}
