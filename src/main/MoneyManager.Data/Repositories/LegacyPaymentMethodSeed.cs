using MoneyManager.Core.Models;

namespace MoneyManager.Data.Repositories
{
	/// <summary>
	/// Payment method seed aligned with MockData / client mocks.
	/// </summary>
	public static class LegacyPaymentMethodSeed
	{
		public static IReadOnlyList<PaymentMethod> PaymentMethods { get; } = new List<PaymentMethod>
		{
			new PaymentMethod { ID = 1, PaymentMethodName = "Discover Checking" },
			new PaymentMethod { ID = 2, PaymentMethodName = "Discover Savings" },
			new PaymentMethod { ID = 3, PaymentMethodName = "Discover Credit" },
			new PaymentMethod { ID = 4, PaymentMethodName = "Arvest Checking" },
			new PaymentMethod { ID = 5, PaymentMethodName = "ABFCU Checking" },
			new PaymentMethod { ID = 6, PaymentMethodName = "ABFCU Savings" },
			new PaymentMethod { ID = 7, PaymentMethodName = "Bank Transfer" },
		};
	}
}
