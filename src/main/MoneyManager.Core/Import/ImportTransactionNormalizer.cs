using MoneyManager.Core.Models;

namespace MoneyManager.Core.Import
{
	public class ImportTransactionNormalizer : IImportTransactionNormalizer
	{
		public BankTransaction Normalize(BankTransaction transaction, ImportSource importSource)
		{
			if (importSource is ImportSource.DiscoverSavings
				or ImportSource.DiscoverChecking
				or ImportSource.AbfcuSavings
				or ImportSource.AbfcuChecking)
			{
				transaction.Amount = -transaction.Amount;
			}

			return transaction;
		}
	}
}
