using MoneyManager.Core.Models;

namespace MoneyManager.Core.Import
{
	/// <summary>
	/// Filters out parsed transactions that already exist as expenses (same date-only and amount rounded to 2 decimals).
	/// Caller should scope existingExpenses to the import's payment method so cross-account matches are ignored.
	/// </summary>
	public class ImportDuplicateFilter : IImportDuplicateFilter
	{
		public IReadOnlyList<BankTransaction> FilterDuplicates(
			IReadOnlyList<Expense> existingExpenses,
			IReadOnlyList<BankTransaction> transactions)
		{
			var existingSet = new HashSet<(DateTime DateOnly, decimal AmountR2)>(
				existingExpenses.Select(e => (e.ExpenseDate.Date, Math.Round(e.Amount, 2))));
			return transactions
				.Where(t => !existingSet.Contains((t.Date.Date, Math.Round(t.Amount, 2))))
				.ToList();
		}
	}
}
