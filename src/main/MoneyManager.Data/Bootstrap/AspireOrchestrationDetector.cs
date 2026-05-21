using Microsoft.Extensions.Configuration;

namespace MoneyManager.Data.Bootstrap;

internal static class AspireOrchestrationDetector
{
	public static bool IsRunningUnderAspire(IConfiguration configuration) =>
		configuration.GetValue("Data:AspireOrchestrated", false)
		|| !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"))
		|| !string.IsNullOrEmpty(configuration["DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"]);
}
