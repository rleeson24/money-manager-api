using MoneyManager.Core.Models;

namespace MoneyManager.Core.UseCases.Expenses
{
	/// <summary>
	/// Result of an expense update (PUT or PATCH). Distinguishes success, not found, and concurrency conflict.
	/// </summary>
	public class UpdateExpenseResult
	{
		public Expense? Updated { get; init; }
		public Expense? ConflictCurrent { get; init; }

		public bool IsSuccess => Updated != null;
		public bool IsConflict => ConflictCurrent != null;
		public bool IsNotFound => Updated == null && ConflictCurrent == null;

		public static UpdateExpenseResult Success(Expense expense) => new() { Updated = expense };
		public static UpdateExpenseResult NotFound() => new() { };
		public static UpdateExpenseResult Conflict(Expense current) => new() { ConflictCurrent = current };
	}
}
