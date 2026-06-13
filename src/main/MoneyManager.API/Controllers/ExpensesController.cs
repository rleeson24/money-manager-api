using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Filters;
using MoneyManager.API.Infrastructure;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Application.Expenses.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using System.Text.Json;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[RequireUserId]
	[Route("api/expenses")]
	public class ExpensesController : ControllerBase
	{
		private readonly IMediator _mediator;

		public ExpensesController(IMediator mediator)
		{
			_mediator = mediator;
		}

		[HttpGet]
		public async Task<IActionResult> GetExpenses(
			[FromQuery] string? month = null,
			[FromQuery] int? paymentMethod = null,
			[FromQuery] bool? datePaidNull = null,
			[FromQuery] string? currency = null,
			CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var expenses = await _mediator.Send(
				new GetExpensesQuery(userId, month, paymentMethod, datePaidNull, currency),
				cancellationToken);

			return Ok(expenses);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetExpense(int id, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var expense = await _mediator.Send(new GetExpenseQuery(id, userId), cancellationToken);
			if (expense != null)
				return Ok(expense);
			return ApiResults.NotFound("Expense not found.");
		}

		[HttpPost]
		public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseModel model, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var expense = await _mediator.Send(new CreateExpenseCommand(userId, model), cancellationToken);
			if (expense != null)
				return CreatedAtAction(nameof(GetExpense), new { id = expense.Expense_I }, expense);
			return ApiResults.UnexpectedFailure();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateExpense(int id, [FromBody] Expense model, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var result = await _mediator.Send(new UpdateExpenseCommand(id, userId, model), cancellationToken);
			if (result.IsSuccess)
				return Ok(result.Updated);
			if (result.IsConflict)
				return ApiResults.Conflict("The expense was modified by another request.", result.ConflictCurrent);
			return ApiResults.NotFound("Expense not found.");
		}

		[HttpPatch("{id}")]
		public async Task<IActionResult> PatchExpense(int id, [FromBody] JsonElement jsonElement, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);

			var result = await _mediator.Send(
				new PatchExpenseCommand(id, userId, jsonElement),
				cancellationToken);
			if (result.IsSuccess)
				return Ok(result.Updated);
			if (result.IsConflict)
				return ApiResults.Conflict("The expense was modified by another request.", result.ConflictCurrent);
			return ApiResults.NotFound("Expense not found.");
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteExpense(int id, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var success = await _mediator.Send(new DeleteExpenseCommand(id, userId), cancellationToken);
			if (success)
				return Ok();
			return ApiResults.NotFound("Expense not found.");
		}

		[HttpPatch("bulk")]
		public async Task<IActionResult> BulkUpdateExpenses([FromBody] BulkUpdateRequest request, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var success = await _mediator.Send(
				new BulkUpdateExpensesCommand(userId, request),
				cancellationToken);
			if (success)
				return Ok();
			return ApiResults.UnexpectedFailure();
		}

		[HttpDelete("bulk")]
		public async Task<IActionResult> BulkDeleteExpenses([FromBody] BulkDeleteRequest request, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);
			var success = await _mediator.Send(new BulkDeleteExpensesCommand(request.Ids, userId), cancellationToken);
			if (success)
				return Ok();
			return ApiResults.UnexpectedFailure();
		}
	}
}
