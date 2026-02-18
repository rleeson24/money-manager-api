using System.Security.Claims;

namespace MoneyManager.API.Utilities
{
	public interface IResolveUserId
	{
		Guid? Resolve(ClaimsPrincipal user);
	}

	public class ResolveUserId : IResolveUserId
	{
		public Guid? Resolve(ClaimsPrincipal user)
		{
			return Guid.NewGuid();
			var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
				?? user.FindFirst("sub")?.Value
				?? user.FindFirst("oid")?.Value;

			if (string.IsNullOrEmpty(userIdClaim))
				return null;

			if (Guid.TryParse(userIdClaim, out var userId))
				return userId;

			return null;
		}
	}
}
