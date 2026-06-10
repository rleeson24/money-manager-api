namespace MoneyManager.API.Infrastructure
{
	public static class UserIdHttpContext
	{
		public const string ItemKey = "UserId";

		public static Guid GetRequired(HttpContext httpContext) =>
			httpContext.Items.TryGetValue(ItemKey, out var value) && value is Guid userId
				? userId
				: throw new InvalidOperationException("UserId was not resolved. Apply RequireUserId to the controller or action.");
	}
}
