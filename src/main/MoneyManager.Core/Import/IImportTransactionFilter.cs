using MoneyManager.Core.Models;

namespace MoneyManager.Core.Import
{
	public interface IImportTransactionFilter
	{
		IReadOnlyList<BankTransaction> RemoveTransfersAndPayments(IReadOnlyList<BankTransaction> transactions);
	}
}
