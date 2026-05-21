using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace MoneyManager.API.Utilities
{
	public interface IResolveUserId
	{
		Guid? Resolve(ClaimsPrincipal user);
	}

	public class ResolveUserId : IResolveUserId
	{
		private const string DefaultAspireSeedUserId = "11111111-1111-1111-1111-111111111111";

		private readonly IConfiguration _configuration;

		public ResolveUserId(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public Guid? Resolve(ClaimsPrincipal user)
		{
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
				?? user.FindFirst("sub")?.Value
				?? user.FindFirst("oid")?.Value;

			if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
				return userId;

			if (IsAspireOrchestrated())
			{
				var seed = _configuration["Data:AspireSeedUserId"] ?? DefaultAspireSeedUserId;
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
