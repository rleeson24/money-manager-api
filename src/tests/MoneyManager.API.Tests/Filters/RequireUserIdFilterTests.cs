using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using MoneyManager.API.Filters;
using MoneyManager.API.Infrastructure;
using MoneyManager.API.Utilities;

namespace MoneyManager.API.Tests.Filters;

public class RequireUserIdFilterTests
{
	[Fact]
	public async Task OnActionExecutionAsync_WhenUserIdResolved_SetsItemAndInvokesNext()
	{
		var userId = Guid.NewGuid();
		var resolveUserId = new Mock<IResolveUserId>();
		resolveUserId.Setup(r => r.Resolve(It.IsAny<ClaimsPrincipal>())).Returns(userId);

		var filter = new RequireUserIdFilter(resolveUserId.Object);
		var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("test")) };
		var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
		var executingContext = new ActionExecutingContext(
			actionContext,
			new List<IFilterMetadata>(),
			new Dictionary<string, object?>(),
			controller: null!);

		var nextInvoked = false;
		ActionExecutionDelegate next = () =>
		{
			nextInvoked = true;
			return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));
		};

		await filter.OnActionExecutionAsync(executingContext, next);

		Assert.Null(executingContext.Result);
		Assert.True(nextInvoked);
		Assert.Equal(userId, httpContext.Items[UserIdHttpContext.ItemKey]);
	}

	[Fact]
	public async Task OnActionExecutionAsync_WhenUserIdNotResolved_ReturnsUnauthorized()
	{
		var resolveUserId = new Mock<IResolveUserId>();
		resolveUserId.Setup(r => r.Resolve(It.IsAny<ClaimsPrincipal>())).Returns((Guid?)null);

		var filter = new RequireUserIdFilter(resolveUserId.Object);
		var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal() };
		var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
		var executingContext = new ActionExecutingContext(
			actionContext,
			new List<IFilterMetadata>(),
			new Dictionary<string, object?>(),
			controller: null!);

		var nextInvoked = false;
		ActionExecutionDelegate next = () =>
		{
			nextInvoked = true;
			return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));
		};

		await filter.OnActionExecutionAsync(executingContext, next);

		Assert.IsType<UnauthorizedObjectResult>(executingContext.Result);
		Assert.False(nextInvoked);
	}
}
