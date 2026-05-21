namespace MoneyManager.Core.Models
{
	/// <summary>
	/// Identifies which bank account / file format an import targets.
	/// </summary>
	public enum ImportSource
	{
		Arvest,
		AbfcuSavings,
		AbfcuChecking,
		DiscoverSavings,
		DiscoverChecking,
		DiscoverCredit
	}

	public static class ImportSourceExtensions
	{
		public static string ToSourceKey(this ImportSource source) => source switch
		{
			ImportSource.Arvest => "Arvest",
			ImportSource.AbfcuSavings => "ABFCU Savings",
			ImportSource.AbfcuChecking => "ABFCU Checking",
			ImportSource.DiscoverSavings => "Discover Savings",
			ImportSource.DiscoverChecking => "Discover Checking",
			ImportSource.DiscoverCredit => "Discover Credit",
			_ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
		};

		public static BankAccountType ToAccountType(this ImportSource source) =>
			source == ImportSource.DiscoverCredit ? BankAccountType.CreditCard : BankAccountType.Depository;
	}
}
