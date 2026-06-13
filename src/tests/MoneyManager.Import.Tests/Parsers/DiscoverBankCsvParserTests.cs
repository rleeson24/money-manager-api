using MoneyManager.Core.Models;
using MoneyManager.Import.Parsers;

namespace MoneyManager.Import.Tests.Parsers
{
	public class DiscoverBankCsvParserTests
	{
		private readonly DiscoverBankCsvParser _parser = new();

		[Fact]
		public void SourceKeys_ContainsDiscoverBankAccounts()
		{
			Assert.Contains("Discover Savings", _parser.SourceKeys);
			Assert.Contains("Discover Checking", _parser.SourceKeys);
		}

		[Fact]
		public async Task ParseAsync_SavingsFixture_ReturnsExpectedTransactions()
		{
			await using var stream = CsvFixtureHelper.OpenFixture("DiscoverSavings.csv");

			var transactions = await _parser.ParseAsync(stream);

			Assert.Equal(2, transactions.Count);

			Assert.Equal(new DateTime(2025, 1, 10), transactions[0].Date);
			Assert.Equal("Interest Payment", transactions[0].Description);
			Assert.Equal(5.25m, transactions[0].Amount);
			Assert.Equal(BankAccountType.Depository, transactions[0].AccountType);

			Assert.Equal(new DateTime(2025, 1, 11), transactions[1].Date);
			Assert.Equal("Online Transfer", transactions[1].Description);
			Assert.Equal(-200.00m, transactions[1].Amount);
		}

		[Fact]
		public async Task ParseAsync_CheckingFixture_ReturnsExpectedTransactions()
		{
			await using var stream = CsvFixtureHelper.OpenFixture("DiscoverChecking.csv");

			var transactions = await _parser.ParseAsync(stream);

			Assert.Equal(2, transactions.Count);
			Assert.Equal(500.00m, transactions[0].Amount);
			Assert.Equal(-32.15m, transactions[1].Amount);
		}

		[Fact]
		public async Task ParseAsync_InvalidHeader_ReturnsEmpty()
		{
			const string csv = "Date,Memo\n01/01/2025,test\n";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Empty(transactions);
		}

		[Fact]
		public async Task ParseAsync_InvalidRowsSkipped()
		{
			const string csv = """
				Transaction Date,Transaction Description,Transaction Type,Debit,Credit,Balance
				not-a-date,Bad Row,Transfer,10.00,,100.00
				06/01/2025,Good Row,Deposit,,25.00,125.00
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal("Good Row", transactions[0].Description);
			Assert.Equal(25.00m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_DebitProducesNegativeAmount()
		{
			const string csv = """
				Transaction Date,Transaction Description,Transaction Type,Debit,Credit,Balance
				07/01/2025,Purchase,Purchase,42.00,,958.00
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(-42.00m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_CreditProducesPositiveAmount()
		{
			const string csv = """
				Transaction Date,Transaction Description,Transaction Type,Debit,Credit,Balance
				07/02/2025,Deposit,Deposit,,99.99,1057.99
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(99.99m, transactions[0].Amount);
		}
	}
}
