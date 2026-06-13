using MoneyManager.Core.Models;

namespace MoneyManager.Core.Import
{
	public interface IImportDuplicateFilter
	{
		IReadOnlyList<BankTransaction> FilterDuplicates(
			IReadOnlyList<Expense> existingExpenses,
			IReadOnlyList<BankTransaction> transactions);
	}
}
