using MoneyManager.Core.Models;

namespace MoneyManager.Core.Import
{
	public static class ImportFilterRules
	{
		private static readonly string[] ExcludedDescriptionFragments =
		[
			"INTERNET PAYMENT - THANK YOU",
			"EDI PYMNTS",
			"Discover (CONA)  NET/MOBILE ROBERT LEESON"
		];

		public static IReadOnlyList<BankTransaction> RemoveTransfersAndPayments(IReadOnlyList<BankTransaction> transactions)
		{
			return transactions.Where(t => !IsExcluded(t.Description)).ToList();
		}

		private static bool IsExcluded(string description)
		{
			foreach (var fragment in ExcludedDescriptionFragments)
			{
				if (description.Contains(fragment, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}
	}
}
