using MoneyManager.Core.Import;
using MoneyManager.Core.Models;
using Xunit;

namespace MoneyManager.Core.Tests.Import;

public class ImportTransactionFilterTests
{
	private readonly ImportTransactionFilter _filter = new();

	[Fact]
	public void RemoveTransfersAndPayments_ExcludesPaymentDescriptions()
	{
		var transactions = new List<BankTransaction>
		{
			new() { Date = DateTime.Today, Amount = -100m, Description = "INTERNET PAYMENT - THANK YOU" },
			new() { Date = DateTime.Today, Amount = -50m, Description = "Grocery store" }
		};

		var result = _filter.RemoveTransfersAndPayments(transactions);

		Assert.Single(result);
		Assert.Equal("Grocery store", result[0].Description);
	}

	[Fact]
	public void RemoveTransfersAndPayments_ExcludesEdiPaymentsCaseInsensitive()
	{
		var transactions = new List<BankTransaction>
		{
			new() { Date = DateTime.Today, Amount = -25m, Description = "edi pymnts transfer" },
			new() { Date = DateTime.Today, Amount = -10m, Description = "Coffee" }
		};

		var result = _filter.RemoveTransfersAndPayments(transactions);

		Assert.Single(result);
		Assert.Equal("Coffee", result[0].Description);
	}

	[Fact]
	public void RemoveTransfersAndPayments_KeepsUnmatchedDescriptions()
	{
		var transactions = new List<BankTransaction>
		{
			new() { Date = DateTime.Today, Amount = -15m, Description = "Restaurant" },
			new() { Date = DateTime.Today, Amount = -8m, Description = "Parking" }
		};

		var result = _filter.RemoveTransfersAndPayments(transactions);

		Assert.Equal(2, result.Count);
	}
}
