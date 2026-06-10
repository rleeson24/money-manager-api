using Microsoft.AspNetCore.Mvc;

namespace MoneyManager.API.Filters
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public sealed class RequireUserIdAttribute : TypeFilterAttribute
	{
		public RequireUserIdAttribute() : base(typeof(RequireUserIdFilter))
		{
		}
	}
}
