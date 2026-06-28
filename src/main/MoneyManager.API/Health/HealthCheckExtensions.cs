using MoneyManager.Data;
using MoneyManager.Data.Bootstrap;

namespace MoneyManager.API.Health;

public static class HealthCheckExtensions
{
	public static WebApplicationBuilder AddMoneyManagerHealthChecks(this WebApplicationBuilder builder)
	{
		var configuration = builder.Configuration;
		var dataOptions = configuration.GetSection("Data").Get<DataOptions>() ?? new DataOptions();
		var connectionString = configuration.GetConnectionString("DefaultConnection");
		var runningUnderAspire = AspireOrchestrationDetector.IsRunningUnderAspire(configuration);

		if (dataOptions.UseInMemoryDatabase && string.IsNullOrEmpty(connectionString) && !runningUnderAspire)
		{
			return builder;
		}

		if (string.IsNullOrEmpty(connectionString) && !runningUnderAspire)
		{
			return builder;
		}

		var includeDatabaseInReadiness = configuration.GetValue("HealthChecks:IncludeDatabaseInReadiness", false);
		var tags = includeDatabaseInReadiness ? new[] { "db", "ready" } : new[] { "db" };

		builder.Services.AddHealthChecks()
			.AddCheck<SqlConnectionHealthCheck>("sqlserver", tags: tags);

		return builder;
	}
}
