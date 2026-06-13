using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MoneyManager.API.Controllers;
using MoneyManager.Core.Application.PaymentMethods.Queries;
using MoneyManager.Core.Models;

namespace MoneyManager.API.Tests.Controllers;

public class PaymentMethodsControllerTests
{
	private readonly Mock<IMediator> _mediator = ControllerTestHelper.CreateMediatorMock();
	private readonly PaymentMethodsController _controller;

	public PaymentMethodsControllerTests()
	{
		_controller = ControllerTestHelper.CreateController<PaymentMethodsController>(_mediator.Object);
	}

	[Fact]
	public async Task GetPaymentMethods_ReturnsOkWithPaymentMethods()
	{
		var paymentMethods = new List<PaymentMethod>
		{
			new() { ID = 1, PaymentMethodName = "Visa" },
			new() { ID = 2, PaymentMethodName = "Checking" }
		};
		_mediator
			.Setup(m => m.Send(It.IsAny<GetPaymentMethodsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(paymentMethods);

		var result = await _controller.GetPaymentMethods();

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(paymentMethods, ok.Value);
	}
}
