using MoneyManager.Core.Expenses;
using Xunit;

namespace MoneyManager.Core.Tests.Expenses;

public class ExpenseConcurrencyTests
{
	[Fact]
	public void ModifiedUtcMillisEqual_ReturnsTrueForSameMillisecond()
	{
		var db = new DateTime(2025, 6, 10, 12, 30, 45, 123, DateTimeKind.Unspecified);
		var client = new DateTime(2025, 6, 10, 12, 30, 45, 123, DateTimeKind.Utc);

		Assert.True(ExpenseConcurrency.ModifiedUtcMillisEqual(db, client));
	}

	[Fact]
	public void ModifiedUtcMillisEqual_ReturnsFalseForDifferentMillisecond()
	{
		var db = new DateTime(2025, 6, 10, 12, 30, 45, 123, DateTimeKind.Unspecified);
		var client = new DateTime(2025, 6, 10, 12, 30, 45, 124, DateTimeKind.Utc);

		Assert.False(ExpenseConcurrency.ModifiedUtcMillisEqual(db, client));
	}

	[Fact]
	public void NormalizeModifiedUtcTicks_TruncatesSubMillisecondPrecision()
	{
		var value = new DateTime(2025, 6, 10, 12, 0, 0, 500, 999, DateTimeKind.Utc);
		var expected = new DateTime(2025, 6, 10, 12, 0, 0, 500, DateTimeKind.Utc).Ticks;

		Assert.Equal(expected, ExpenseConcurrency.NormalizeModifiedUtcTicks(value));
	}

	[Fact]
	public void NormalizeModifiedUtcTicks_TreatsUnspecifiedAsUtcWallClock()
	{
		var unspecified = new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Unspecified);
		var utc = new DateTime(2025, 6, 10, 8, 0, 0, DateTimeKind.Utc);

		Assert.Equal(
			ExpenseConcurrency.NormalizeModifiedUtcTicks(unspecified),
			ExpenseConcurrency.NormalizeModifiedUtcTicks(utc));
	}
}
