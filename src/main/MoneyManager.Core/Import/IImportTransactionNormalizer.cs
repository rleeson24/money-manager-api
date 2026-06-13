using MoneyManager.Core.Models;

namespace MoneyManager.Core.Import
{
	public interface IImportTransactionNormalizer
	{
		BankTransaction Normalize(BankTransaction transaction, ImportSource importSource);
	}
}
