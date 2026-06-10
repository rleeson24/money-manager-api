using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MoneyManager.Core.Constants;
using MoneyManager.Data.Bootstrap;

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
			if (user.Identity?.IsAuthenticated != true && AspireOrchestrationDetector.IsRunningUnderAspire(_configuration))
			{
				var seed = _configuration["Data:AspireSeedUserId"] ?? AspireConstants.DefaultSeedUserId;
				if (Guid.TryParse(seed, out var aspireSeed))
					return aspireSeed;
			}

			return null;
		}

	}
}
