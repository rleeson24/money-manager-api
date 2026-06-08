using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MoneyManager.Core.Constants;

namespace MoneyManager.API.Utilities
{
	public interface IResolveUserId
	{
		Guid? Resolve(ClaimsPrincipal user);
	}

	public class ResolveUserId : IResolveUserId
	{
		private readonly IConfiguration _configuration;

		public ResolveUserId(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public Guid? Resolve(ClaimsPrincipal user)
		{
			// Azure AD object ID is the stable identifier for row-level scoping
			var userIdClaim = user.FindFirst("oid")?.Value
				?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
				?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
				?? user.FindFirst("sub")?.Value;

			if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
				return userId;

			// Dev-only fallback when running under Aspire without an Azure AD token
			if (user.Identity?.IsAuthenticated != true && IsAspireOrchestrated())
			{
				var seed = _configuration["Data:AspireSeedUserId"] ?? AspireConstants.DefaultSeedUserId;
				if (Guid.TryParse(seed, out var aspireSeed))
					return aspireSeed;
			}

			return null;
		}

		private bool IsAspireOrchestrated() =>
			_configuration.GetValue("Data:AspireOrchestrated", false)
			|| !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"))
			|| !string.IsNullOrEmpty(_configuration["DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"]);
	}
}
