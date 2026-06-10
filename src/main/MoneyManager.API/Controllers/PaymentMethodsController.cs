using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Infrastructure;
using MoneyManager.Core.Application.PaymentMethods.Queries;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[Route("api/payment-methods")]
	public class PaymentMethodsController : ControllerBase
	{
		private readonly IMediator _mediator;

		public PaymentMethodsController(IMediator mediator)
		{
			_mediator = mediator;
		}

		[HttpGet]
		public async Task<IActionResult> GetPaymentMethods(CancellationToken cancellationToken = default)
		{
			var paymentMethods = await _mediator.Send(new GetPaymentMethodsQuery(), cancellationToken);
			return Ok(paymentMethods);
		}
	}
}
