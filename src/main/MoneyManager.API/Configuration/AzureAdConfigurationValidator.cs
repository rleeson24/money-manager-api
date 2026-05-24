namespace MoneyManager.API.Configuration;

public static class AzureAdConfigurationValidator
{
	public static void LogConfigurationStatus(IConfiguration configuration, ILogger logger)
	{
		var tenantId = configuration["AzureAd:TenantId"];
		var clientId = configuration["AzureAd:ClientId"];
		var audience = configuration["AzureAd:Audience"];
		var allowedOrigins = configuration["AllowedOrigins"];

		if (string.IsNullOrWhiteSpace(tenantId)
			|| string.IsNullOrWhiteSpace(clientId)
			|| string.IsNullOrWhiteSpace(audience))
		{
			logger.LogWarning(
				"Azure AD is not fully configured. Set AzureAd:TenantId, AzureAd:ClientId, and AzureAd:Audience. All API endpoints require authentication.");
			return;
		}

		logger.LogInformation(
			"Azure AD configured for tenant {TenantId}, audience {Audience}.",
			tenantId,
			audience);

		if (string.IsNullOrWhiteSpace(allowedOrigins))
		{
			logger.LogWarning(
				"AllowedOrigins is empty. Browser clients may be blocked by CORS outside Development/Local environments.");
		}

		if (!string.IsNullOrWhiteSpace(configuration["AzureAd:Issuer"]))
		{
			logger.LogWarning(
				"AzureAd:Issuer is set and overrides default issuer validation. Remove it unless you intentionally restrict token issuers.");
		}
	}
}
