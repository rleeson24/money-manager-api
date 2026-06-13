using MoneyManager.Core.Models;
using MoneyManager.Import.Parsers;

namespace MoneyManager.Import.Tests.Parsers
{
	public class DiscoverCreditCsvParserTests
	{
		private readonly DiscoverCreditCsvParser _parser = new();

		[Fact]
		public void SourceKeys_ContainsDiscoverCredit()
		{
			Assert.Contains("Discover Credit", _parser.SourceKeys);
		}

		[Fact]
		public async Task ParseAsync_ValidFixture_ReturnsExpectedTransactions()
		{
			await using var stream = CsvFixtureHelper.OpenFixture("DiscoverCredit.csv");

			var transactions = await _parser.ParseAsync(stream);

			Assert.Equal(2, transactions.Count);

			Assert.Equal(new DateTime(2025, 1, 5), transactions[0].Date);
			Assert.Equal("AMAZON.COM", transactions[0].Description);
			Assert.Equal(-59.99m, transactions[0].Amount);
			Assert.Equal(BankAccountType.CreditCard, transactions[0].AccountType);

			Assert.Equal(new DateTime(2025, 1, 7), transactions[1].Date);
			Assert.Equal("PAYMENT - THANK YOU", transactions[1].Description);
			Assert.Equal(500.00m, transactions[1].Amount);
		}

		[Fact]
		public async Task ParseAsync_InvalidHeader_ReturnsEmpty()
		{
			const string csv = "Date,Description\n01/01/2025,test\n";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Empty(transactions);
		}

		[Fact]
		public async Task ParseAsync_InvalidRowsSkipped()
		{
			const string csv = """
				Trans. Date,Post Date,Description,Amount,Category
				01/01/2025,01/02/2025,Bad Amount,not-a-number,Other
				01/03/2025,01/04/2025,Valid Purchase,-10.00,Merchandise
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal("Valid Purchase", transactions[0].Description);
			Assert.Equal(-10.00m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_UsesPostDateWhenTransDateMissing()
		{
			const string csv = """
				Post Date,Description,Amount,Category
				08/15/2025,Post Date Only,-22.00,Merchandise
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(new DateTime(2025, 8, 15), transactions[0].Date);
			Assert.Equal(-22.00m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_NegativePurchase_RemainsNegative()
		{
			const string csv = """
				Trans. Date,Post Date,Description,Amount,Category
				09/01/2025,09/02/2025,Store,-15.75,Merchandise
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(-15.75m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_PositivePayment_RemainsPositive()
		{
			const string csv = """
				Trans. Date,Post Date,Description,Amount,Category
				09/03/2025,09/04/2025,Payment,250.00,Payments
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(250.00m, transactions[0].Amount);
		}
	}
}
