using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Core.UseCases.PaymentMethods;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Route("api/payment-methods")]
	[Authorize]
	public class PaymentMethodsController : ControllerBase
	{
		[HttpGet]
		public async Task<IActionResult> GetPaymentMethods([FromServices] IGetPaymentMethodsUseCase getPaymentMethodsUseCase)
		{
			var paymentMethods = await getPaymentMethodsUseCase.Execute();
			if (paymentMethods != null)
			{
				return Ok(paymentMethods);
			}
			return Problem();
		}
	}
}
