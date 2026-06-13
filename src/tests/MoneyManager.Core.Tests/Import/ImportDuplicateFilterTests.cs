using MoneyManager.Core.Import;
using MoneyManager.Core.Models;
using Xunit;

namespace MoneyManager.Core.Tests.Import;

public class ImportDuplicateFilterTests
{
	private readonly ImportDuplicateFilter _filter = new();
	[Fact]
	public void FilterDuplicates_RemovesMatchingDateAndAmount()
	{
		var date = new DateTime(2025, 3, 15);
		var existing = new List<Expense>
		{
			new() { ExpenseDate = date, Amount = 25.50m }
		};
		var transactions = new List<BankTransaction>
		{
			new() { Date = date, Amount = 25.50m, Description = "Duplicate" },
			new() { Date = date, Amount = 10.00m, Description = "Unique" }
		};

		var result = _filter.FilterDuplicates(existing, transactions);

		Assert.Single(result);
		Assert.Equal(10.00m, result[0].Amount);
	}

	[Fact]
	public void FilterDuplicates_KeepsAllWhenNoMatches()
	{
		var existing = new List<Expense>
		{
			new() { ExpenseDate = new DateTime(2025, 1, 1), Amount = 100m }
		};
		var transactions = new List<BankTransaction>
		{
			new() { Date = new DateTime(2025, 2, 1), Amount = 50m, Description = "A" },
			new() { Date = new DateTime(2025, 2, 2), Amount = 75m, Description = "B" }
		};

		var result = _filter.FilterDuplicates(existing, transactions);

		Assert.Equal(2, result.Count);
	}

	[Fact]
	public void FilterDuplicates_IgnoresTimeComponentOnDates()
	{
		var existing = new List<Expense>
		{
			new() { ExpenseDate = new DateTime(2025, 3, 15, 14, 30, 0), Amount = 20m }
		};
		var transactions = new List<BankTransaction>
		{
			new() { Date = new DateTime(2025, 3, 15, 8, 0, 0), Amount = 20m, Description = "Same day" }
		};

		var result = _filter.FilterDuplicates(existing, transactions);

		Assert.Empty(result);
	}
}
