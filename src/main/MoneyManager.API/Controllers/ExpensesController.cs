using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Utilities;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.UseCases.Expenses;
using System.Text.Json;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Route("api/expenses")]
	[Authorize]
	public class ExpensesController : ControllerBase
	{
		private readonly IResolveUserId _resolveUserId;

		public ExpensesController(IResolveUserId resolveUserId)
		{
			_resolveUserId = resolveUserId;
		}

		[HttpGet]
		public async Task<IActionResult> GetExpenses(
			[FromServices] IGetExpensesUseCase getExpensesUseCase,
			[FromQuery] string? month = null,
			[FromQuery] int? paymentMethod = null,
			[FromQuery] bool? datePaidNull = null)
		{
			var userId = _resolveUserId.Resolve(User);

			IReadOnlyList<Expense>? expenses;
			if (paymentMethod.HasValue || datePaidNull.HasValue)
			{
				expenses = await getExpensesUseCase.ExecuteWithFilters(userId.Value, paymentMethod, datePaidNull);
			}
			else
			{
				expenses = await getExpensesUseCase.Execute(userId.Value, month);
			}

			if (expenses != null)
			{
				return Ok(expenses);
			}
			return Problem();
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetExpense(
			[FromServices] IGetExpenseUseCase getExpenseUseCase,
			int id)
		{
			var userId = _resolveUserId.Resolve(User);

			var expense = await getExpenseUseCase.Execute(id, userId.Value);
			if (expense != null)
			{
				return Ok(expense);
			}
			return NotFound();
		}

		[HttpPost]
		public async Task<IActionResult> CreateExpense(
			[FromServices] ICreateExpenseUseCase createExpenseUseCase,
			[FromBody] CreateExpenseModel model)
		{
			var userId = _resolveUserId.Resolve(User);

			var expense = await createExpenseUseCase.Execute(userId.Value, model);
			if (expense != null)
			{
				return CreatedAtAction(nameof(GetExpense), new { id = expense.Expense_I }, expense);
			}
			return Problem();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateExpense(
			[FromServices] IUpdateExpenseUseCase updateExpenseUseCase,
			int id,
			[FromBody] Expense model)
		{
			var userId = _resolveUserId.Resolve(User);

			var result = await updateExpenseUseCase.Execute(id, userId.Value, model);
			if (result.IsSuccess)
				return Ok(result.Updated);
			if (result.IsConflict)
				return Conflict(result.ConflictCurrent);
			return NotFound();
		}

		[HttpPatch("{id}")]
		public async Task<IActionResult> PatchExpense(
			[FromServices] IPatchExpenseUseCase patchExpenseUseCase,
			int id,
			[FromBody] JsonElement jsonElement)
		{
			var userId = _resolveUserId.Resolve(User);

			var updates = new Dictionary<string, object?>();
			DateTime? expectedModifiedDateTime = null;
			foreach (var prop in jsonElement.EnumerateObject())
			{
				var key = prop.Name;
				var value = prop.Value;

				// Used for optimistic concurrency; not a column update
				if (key == "ModifiedDateTime")
				{
					expectedModifiedDateTime = ParseDateTimeFromElement(value);
					continue;
				}
				// Never persist CreatedDateTime from PATCH body
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
						{
							updates[key] = dateValue;
						}
					}
					else
					{
						updates[key] = strValue;
					}
				}
				else if (value.ValueKind == JsonValueKind.Number)
				{
					if (key == "Amount" || key == "PaymentMethod")
					{
						if (value.TryGetInt32(out var intValue))
						{
							updates[key] = intValue;
						}
						else if (value.TryGetDecimal(out var decimalValue))
						{
							updates[key] = decimalValue;
						}
					}
				}
			}

			var result = await patchExpenseUseCase.Execute(id, userId.Value, updates, expectedModifiedDateTime);
			if (result.IsSuccess)
				return Ok(result.Updated);
			if (result.IsConflict)
				return Conflict(result.ConflictCurrent);
			return NotFound();
		}

		private static DateTime? ParseDateTimeFromElement(JsonElement value)
		{
			if (value.ValueKind == JsonValueKind.Null) return null;
			if (value.ValueKind == JsonValueKind.String && value.GetString() is { } s && DateTime.TryParse(s, out var d))
				return d;
			return null;
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteExpense(
			[FromServices] IDeleteExpenseUseCase deleteExpenseUseCase,
			int id)
		{
			var userId = _resolveUserId.Resolve(User);

			var success = await deleteExpenseUseCase.Execute(id, userId.Value);
			if (success)
			{
				return Ok();
			}
			return NotFound();
		}

		[HttpPatch("bulk")]
		public async Task<IActionResult> BulkUpdateExpenses(
			[FromServices] IBulkUpdateExpensesUseCase bulkUpdateExpensesUseCase,
			[FromBody] BulkUpdateRequest request)
		{
			var userId = _resolveUserId.Resolve(User);

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

			var success = await bulkUpdateExpensesUseCase.Execute(request.Ids, userId.Value, updates);
			if (success)
			{
				return Ok();
			}
			return Problem();
		}

		[HttpDelete("bulk")]
		public async Task<IActionResult> BulkDeleteExpenses(
			[FromServices] IBulkDeleteExpensesUseCase bulkDeleteExpensesUseCase,
			[FromBody] BulkDeleteRequest request)
		{
			var userId = _resolveUserId.Resolve(User);

			var success = await bulkDeleteExpensesUseCase.Execute(request.Ids, userId.Value);
			if (success)
			{
				return Ok();
			}
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
