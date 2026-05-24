using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace MoneyManager.API.Configuration;

/// <summary>
/// Runs after Microsoft.Identity.Web configuration so issuer/audience validation
/// accepts common Azure AD token formats.
/// </summary>
public sealed class AzureAdJwtBearerPostConfigure : IPostConfigureOptions<JwtBearerOptions>
{
	public const string SchemeName = "Microsoft";

	private readonly IConfiguration _configuration;

	public AzureAdJwtBearerPostConfigure(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public void PostConfigure(string? name, JwtBearerOptions options)
	{
		if (!string.Equals(name, SchemeName, StringComparison.Ordinal))
			return;

		var tenantId = _configuration["AzureAd:TenantId"];
		var clientId = _configuration["AzureAd:ClientId"];
		var audience = _configuration["AzureAd:Audience"];

		if (!string.IsNullOrWhiteSpace(tenantId))
		{
			options.TokenValidationParameters.ValidateIssuer = true;
			options.TokenValidationParameters.ValidIssuers =
			[
				$"https://login.microsoftonline.com/{tenantId}/v2.0",
				$"https://sts.windows.net/{tenantId}/"
			];
		}

		var validAudiences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (!string.IsNullOrWhiteSpace(audience))
			validAudiences.Add(audience);
		if (!string.IsNullOrWhiteSpace(clientId))
		{
			validAudiences.Add(clientId);
			validAudiences.Add($"api://{clientId}");
		}

		if (validAudiences.Count > 0)
		{
			options.TokenValidationParameters.ValidateAudience = true;
			options.TokenValidationParameters.ValidAudiences = validAudiences.ToArray();
		}

		options.Events ??= new JwtBearerEvents();
		options.Events.OnAuthenticationFailed = context =>
		{
			var logger = context.HttpContext.RequestServices
				.GetRequiredService<ILogger<AzureAdJwtBearerPostConfigure>>();
			logger.LogWarning(context.Exception, "Azure AD JWT authentication failed.");
			return Task.CompletedTask;
		};
	}
}
