using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Filters;
using MoneyManager.API.Infrastructure;
using MoneyManager.Core.Application.Reports.Queries;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[RequireUserId]
	[Route("api/reports")]
	public class ReportsController : ControllerBase
	{
		private readonly IMediator _mediator;

		public ReportsController(IMediator mediator)
		{
			_mediator = mediator;
		}

		[HttpGet("budget")]
		public async Task<IActionResult> GetBudgetReport([FromQuery] string? month, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var report = await _mediator.Send(new GetBudgetReportQuery(userId, month ?? string.Empty), cancellationToken);
			return Ok(report);
		}
	}
}
