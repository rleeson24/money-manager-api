using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Filters;
using MoneyManager.API.Infrastructure;
using MoneyManager.Core.Application.ExpenseSplits.Commands;
using MoneyManager.Core.Application.ExpenseSplits.Queries;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[RequireUserId]
	[Route("api/expenses")]
	public class ExpenseSplitsController : ControllerBase
	{
		private readonly IMediator _mediator;

		public ExpenseSplitsController(IMediator mediator)
		{
			_mediator = mediator;
		}

		[HttpGet("split")]
		public async Task<IActionResult> GetExpenseSplits([FromQuery] int expenseId, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var splits = await _mediator.Send(new GetExpenseSplitsQuery(expenseId, userId), cancellationToken);
			return Ok(splits);
		}

		[HttpPost("split")]
		public async Task<IActionResult> CreateExpenseSplit([FromBody] CreateOrUpdateExpenseSplitModel model, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var split = await _mediator.Send(new CreateExpenseSplitCommand(userId, model), cancellationToken);
			if (split != null)
				return CreatedAtAction(nameof(GetExpenseSplits), new { expenseId = model.Expense_I }, split);
			return ApiResults.NotFound("Expense not found.");
		}

		[HttpPut("split/{id}")]
		public async Task<IActionResult> UpdateExpenseSplit(int id, [FromBody] CreateOrUpdateExpenseSplitModel model, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var split = await _mediator.Send(new UpdateExpenseSplitCommand(id, userId, model), cancellationToken);
			if (split != null)
				return Ok(split);
			return ApiResults.NotFound("Expense split not found.");
		}

		[HttpDelete("split/{id}")]
		public async Task<IActionResult> DeleteExpenseSplit(int id, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var success = await _mediator.Send(new DeleteExpenseSplitCommand(id, userId), cancellationToken);
			if (success)
				return NoContent();
			return ApiResults.NotFound("Expense split not found.");
		}

		[HttpPut("split/replace")]
		public async Task<IActionResult> ReplaceExpenseSplits(
			[FromQuery] int expenseId,
			[FromBody] ReplaceExpenseSplitsRequest request,
			CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var result = await _mediator.Send(new ReplaceExpenseSplitsCommand(expenseId, userId, request), cancellationToken);
			if (result.IsSuccess)
				return Ok(result.Splits);
			if (result.ValidationError == "Expense not found.")
				return ApiResults.NotFound(result.ValidationError);
			return ApiResults.ValidationError(result.ValidationError ?? "Replace splits failed.");
		}
	}
}
