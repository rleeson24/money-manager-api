using Microsoft.AspNetCore.Mvc.Filters;
using MoneyManager.API.Infrastructure;
using MoneyManager.API.Utilities;

namespace MoneyManager.API.Filters
{
	public class RequireUserIdFilter : IAsyncActionFilter
	{
		private readonly IResolveUserId _resolveUserId;

		public RequireUserIdFilter(IResolveUserId resolveUserId)
		{
			_resolveUserId = resolveUserId;
		}

		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var userId = _resolveUserId.Resolve(context.HttpContext.User);
			if (userId == null)
			{
				context.Result = ApiResults.Unauthorized();
				return;
			}

			context.HttpContext.Items[UserIdHttpContext.ItemKey] = userId.Value;
			await next();
		}
	}
}
