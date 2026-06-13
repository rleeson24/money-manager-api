using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MoneyManager.API.Infrastructure;

namespace MoneyManager.API.Tests;

public static class ControllerTestHelper
{
	public static readonly Guid DefaultUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

	public static Mock<IMediator> CreateMediatorMock() => new(MockBehavior.Strict);

	public static DefaultHttpContext CreateHttpContext(Guid? userId = null)
	{
		var httpContext = new DefaultHttpContext();
		if (userId.HasValue)
			httpContext.Items[UserIdHttpContext.ItemKey] = userId.Value;
		return httpContext;
	}

	public static void AttachHttpContext(ControllerBase controller, Guid? userId = null)
	{
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = CreateHttpContext(userId ?? DefaultUserId)
		};
	}

	public static TController CreateController<TController>(IMediator mediator, Guid? userId = null)
		where TController : ControllerBase
	{
		var controller = (TController)Activator.CreateInstance(typeof(TController), mediator)!;
		if (userId.HasValue || typeof(TController).GetCustomAttributes(typeof(MoneyManager.API.Filters.RequireUserIdAttribute), inherit: true).Length > 0)
			AttachHttpContext(controller, userId);
		return controller;
	}

	public static TController CreateController<TController>(IMediator mediator, object secondDependency, Guid? userId = null)
		where TController : ControllerBase
	{
		var controller = (TController)Activator.CreateInstance(typeof(TController), mediator, secondDependency)!;
		AttachHttpContext(controller, userId);
		return controller;
	}
}
