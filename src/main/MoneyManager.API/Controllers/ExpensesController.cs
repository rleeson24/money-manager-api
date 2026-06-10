using System.Collections.Generic;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Infrastructure;
using MoneyManager.API.Utilities;
using MoneyManager.Core.Application.ExpenseSplits.Commands;
using MoneyManager.Core.Application.ExpenseSplits.Queries;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Application.Expenses.Queries;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using System.Text.Json;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[Route("api/expenses")]
	public class ExpensesController : ControllerBase
	{
		private readonly IResolveUserId _resolveUserId;
		private readonly IMediator _mediator;

		public ExpensesController(IResolveUserId resolveUserId, IMediator mediator)
		{
			_resolveUserId = resolveUserId;
			_mediator = mediator;
		}

		private IActionResult? UnauthorizedIfNoUser(out Guid userId)
		{
			var resolved = _resolveUserId.Resolve(User);
			if (resolved == null)
			{
				userId = default;
				return Unauthorized();
			}
			userId = resolved.Value;
			return null;
		}

		[HttpGet]
		public async Task<IActionResult> GetExpenses(
			[FromQuery] string? month = null,
			[FromQuery] int? paymentMethod = null,
			[FromQuery] bool? datePaidNull = null,
			[FromQuery] string? currency = null,
			CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var expenses = await _mediator.Send(
				new GetExpensesQuery(userId, month, paymentMethod, datePaidNull, currency),
				cancellationToken);

			return Ok(expenses);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetExpense(int id, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var expense = await _mediator.Send(new GetExpenseQuery(id, userId), cancellationToken);
			if (expense != null)
				return Ok(expense);
			return ApiResults.NotFound("Expense not found.");
		}

		[HttpPost]
		public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseModel model, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var expense = await _mediator.Send(new CreateExpenseCommand(userId, model), cancellationToken);
			if (expense != null)
				return CreatedAtAction(nameof(GetExpense), new { id = expense.Expense_I }, expense);
			return ApiResults.UnexpectedFailure();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateExpense(int id, [FromBody] Expense model, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

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
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var parsed = ExpensePatchParser.Parse(jsonElement);

			var result = await _mediator.Send(
				new PatchExpenseCommand(id, userId, parsed.Updates, parsed.ExpectedModifiedDateTime),
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
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var success = await _mediator.Send(new DeleteExpenseCommand(id, userId), cancellationToken);
			if (success)
				return Ok();
			return ApiResults.NotFound("Expense not found.");
		}

		[HttpPatch("bulk")]
		public async Task<IActionResult> BulkUpdateExpenses([FromBody] BulkUpdateRequest request, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var updates = new Dictionary<string, object?>();
			if (request.ExpenseDate != null)
				updates[ExpenseFieldNames.ExpenseDate] = request.ExpenseDate;
			if (request.setCategoryToNull == true)
				updates[ExpenseFieldNames.Category] = null;
			else if (request.Category != null)
				updates[ExpenseFieldNames.Category] = request.Category;
			if (request.setDatePaidToNull == true)
				updates[ExpenseFieldNames.DatePaid] = null;
			else if (request.DatePaid != null)
				updates[ExpenseFieldNames.DatePaid] = request.DatePaid;

			var success = await _mediator.Send(
				new BulkUpdateExpensesCommand(request.Ids, userId, updates),
				cancellationToken);
			if (success)
				return Ok();
			return ApiResults.UnexpectedFailure();
		}

		[HttpGet("split")]
		public async Task<IActionResult> GetExpenseSplits([FromQuery] int expenseId, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;
			var splits = await _mediator.Send(new GetExpenseSplitsQuery(expenseId, userId), cancellationToken);
			return Ok(splits);
		}

		[HttpPost("split")]
		public async Task<IActionResult> CreateExpenseSplit([FromBody] CreateOrUpdateExpenseSplitModel model, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;
			var split = await _mediator.Send(new CreateExpenseSplitCommand(userId, model), cancellationToken);
			if (split != null)
				return CreatedAtAction(nameof(GetExpenseSplits), new { expenseId = model.Expense_I }, split);
			return ApiResults.NotFound("Expense not found.");
		}

		[HttpPut("split/{id}")]
		public async Task<IActionResult> UpdateExpenseSplit(int id, [FromBody] CreateOrUpdateExpenseSplitModel model, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;
			var split = await _mediator.Send(new UpdateExpenseSplitCommand(id, userId, model), cancellationToken);
			if (split != null)
				return Ok(split);
			return ApiResults.NotFound("Expense split not found.");
		}

		[HttpDelete("split/{id}")]
		public async Task<IActionResult> DeleteExpenseSplit(int id, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;
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
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;
			var result = await _mediator.Send(new ReplaceExpenseSplitsCommand(expenseId, userId, request), cancellationToken);
			if (result.IsSuccess)
				return Ok(result.Splits);
			if (result.ValidationError == "Expense not found.")
				return ApiResults.NotFound(result.ValidationError);
			return ApiResults.ValidationError(result.ValidationError ?? "Replace splits failed.");
		}

		[HttpDelete("bulk")]
		public async Task<IActionResult> BulkDeleteExpenses([FromBody] BulkDeleteRequest request, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var success = await _mediator.Send(new BulkDeleteExpensesCommand(request.Ids, userId), cancellationToken);
			if (success)
				return Ok();
			return ApiResults.UnexpectedFailure();
		}
	}

	public class BulkUpdateRequest
	{
		public List<int> Ids { get; set; } = new();
		public DateTime? ExpenseDate { get; set; }
		public int? Category { get; set; }
		public bool? setCategoryToNull { get; set; }
		public DateTime? DatePaid { get; set; }
		public bool? setDatePaidToNull { get; set; }
	}

	public class BulkDeleteRequest
	{
		public List<int> Ids { get; set; } = new();
	}
}
