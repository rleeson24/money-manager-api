using System.Collections.Generic;
using System.Globalization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Utilities;
using MoneyManager.Core.Application.ExpenseSplits.Commands;
using MoneyManager.Core.Application.ExpenseSplits.Queries;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Application.Expenses.Queries;
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

			if (expenses != null)
				return Ok(expenses);
			return Problem();
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetExpense(int id, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var expense = await _mediator.Send(new GetExpenseQuery(id, userId), cancellationToken);
			if (expense != null)
				return Ok(expense);
			return NotFound();
		}

		[HttpPost]
		public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseModel model, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var expense = await _mediator.Send(new CreateExpenseCommand(userId, model), cancellationToken);
			if (expense != null)
				return CreatedAtAction(nameof(GetExpense), new { id = expense.Expense_I }, expense);
			return Problem();
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
				return Conflict(result.ConflictCurrent);
			return NotFound();
		}

		[HttpPatch("{id}")]
		public async Task<IActionResult> PatchExpense(int id, [FromBody] JsonElement jsonElement, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var updates = new Dictionary<string, object?>();
			DateTime? expectedModifiedDateTime = null;
			foreach (var prop in jsonElement.EnumerateObject())
			{
				var key = prop.Name;
				var value = prop.Value;

				if (key == "ModifiedDateTime")
				{
					expectedModifiedDateTime = ParseDateTimeFromElement(value);
					continue;
				}
				if (key == "CreatedDateTime")
					continue;

				if (value.ValueKind == JsonValueKind.Null)
				{
					updates[key] = null;
				}
				else if (value.ValueKind == JsonValueKind.String)
				{
					var strValue = value.GetString();
					if (key == "ExpenseDate" || key == "DatePaid")
					{
						if (DateTime.TryParse(strValue, out var dateValue))
							updates[key] = dateValue;
					}
					else
					{
						updates[key] = strValue;
					}
				}
				else if (value.ValueKind == JsonValueKind.Number)
				{
					if (key == "Amount" || key == "PaymentMethod" || key == "Category")
					{
						if (value.TryGetInt32(out var intValue))
							updates[key] = intValue;
						else if (value.TryGetDecimal(out var decimalValue))
							updates[key] = decimalValue;
					}
				}
				else if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
				{
					var boolKey = key switch
					{
						"isSplit" => "IsSplit",
						"excludeFromCredit" => "ExcludeFromCredit",
						_ => key
					};
					updates[boolKey] = value.GetBoolean();
				}
			}

			var result = await _mediator.Send(
				new PatchExpenseCommand(id, userId, updates, expectedModifiedDateTime),
				cancellationToken);
			if (result.IsSuccess)
				return Ok(result.Updated);
			if (result.IsConflict)
				return Conflict(result.ConflictCurrent);
			return NotFound();
		}

		private static DateTime? ParseDateTimeFromElement(JsonElement value)
		{
			if (value.ValueKind == JsonValueKind.Null) return null;
			if (value.ValueKind == JsonValueKind.String && value.GetString() is { } s)
			{
				return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
					? d
					: null;
			}
			return null;
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteExpense(int id, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var success = await _mediator.Send(new DeleteExpenseCommand(id, userId), cancellationToken);
			if (success)
				return Ok();
			return NotFound();
		}

		[HttpPatch("bulk")]
		public async Task<IActionResult> BulkUpdateExpenses([FromBody] BulkUpdateRequest request, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var updates = new Dictionary<string, object?>();
			if (request.ExpenseDate != null)
				updates["ExpenseDate"] = request.ExpenseDate;
			if (request.setCategoryToNull == true)
				updates["Category"] = null;
			else if (request.Category != null)
				updates["Category"] = request.Category;
			if (request.setDatePaidToNull == true)
				updates["DatePaid"] = null;
			else if (request.DatePaid != null)
				updates["DatePaid"] = request.DatePaid;

			var success = await _mediator.Send(
				new BulkUpdateExpensesCommand(request.Ids, userId, updates),
				cancellationToken);
			if (success)
				return Ok();
			return Problem();
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
			return BadRequest();
		}

		[HttpPut("split/{id}")]
		public async Task<IActionResult> UpdateExpenseSplit(int id, [FromBody] CreateOrUpdateExpenseSplitModel model, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;
			var split = await _mediator.Send(new UpdateExpenseSplitCommand(id, userId, model), cancellationToken);
			if (split != null)
				return Ok(split);
			return NotFound();
		}

		[HttpDelete("split/{id}")]
		public async Task<IActionResult> DeleteExpenseSplit(int id, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;
			var success = await _mediator.Send(new DeleteExpenseSplitCommand(id, userId), cancellationToken);
			if (success)
				return NoContent();
			return NotFound();
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
			return BadRequest(new { error = result.ValidationError });
		}

		[HttpDelete("bulk")]
		public async Task<IActionResult> BulkDeleteExpenses([FromBody] BulkDeleteRequest request, CancellationToken cancellationToken = default)
		{
			if (UnauthorizedIfNoUser(out var userId) is { } unauthorized)
				return unauthorized;

			var success = await _mediator.Send(new BulkDeleteExpensesCommand(request.Ids, userId), cancellationToken);
			if (success)
				return Ok();
			return Problem();
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
