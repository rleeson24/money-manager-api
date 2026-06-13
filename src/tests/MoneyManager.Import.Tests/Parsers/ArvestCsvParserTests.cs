using MoneyManager.Core.Models;
using MoneyManager.Import.Parsers;

namespace MoneyManager.Import.Tests.Parsers
{
	public class ArvestCsvParserTests
	{
		private readonly ArvestCsvParser _parser = new();

		[Fact]
		public void SourceKeys_ContainsArvest()
		{
			Assert.Contains("Arvest", _parser.SourceKeys);
		}

		[Fact]
		public async Task ParseAsync_ValidFixture_ReturnsExpectedTransactions()
		{
			await using var stream = CsvFixtureHelper.OpenFixture("Arvest.csv");

			var transactions = await _parser.ParseAsync(stream);

			Assert.Equal(3, transactions.Count);

			Assert.Equal(new DateTime(2025, 1, 15), transactions[0].Date);
			Assert.Equal("Direct Deposit Payroll", transactions[0].Description);
			Assert.Equal(-1500.00m, transactions[0].Amount);
			Assert.Equal(BankAccountType.Depository, transactions[0].AccountType);

			Assert.Equal(new DateTime(2025, 1, 16), transactions[1].Date);
			Assert.Equal("Grocery Store", transactions[1].Description);
			Assert.Equal(45.67m, transactions[1].Amount);

			Assert.Equal(new DateTime(2025, 1, 18), transactions[2].Date);
			Assert.Equal("Coffee Shop", transactions[2].Description);
			Assert.Equal(5.50m, transactions[2].Amount);
		}

		[Fact]
		public async Task ParseAsync_InvalidHeader_ReturnsEmpty()
		{
			const string csv = "Wrong,Columns,Only\n01/01/2025,foo,bar\n";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Empty(transactions);
		}

		[Fact]
		public async Task ParseAsync_InvalidRowsSkipped()
		{
			const string csv = """
				Account,Date,Pending?,Description,Category,Check,Credit,Debit
				Checking,not-a-date,,"Skipped Row",,,,-10.00
				Checking,02/01/2025,,"Valid Row",,,,-25.00
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(new DateTime(2025, 2, 1), transactions[0].Date);
			Assert.Equal("Valid Row", transactions[0].Description);
			Assert.Equal(25.00m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_CreditColumn_ProducesNegativeAmount()
		{
			const string csv = """
				Account,Date,Pending?,Description,Category,Check,Credit,Debit
				Checking,03/01/2025,,Deposit,,,100.00,
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(-100.00m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_DebitColumn_ProducesPositiveAmountFromNegativeExport()
		{
			const string csv = """
				Account,Date,Pending?,Description,Category,Check,Credit,Debit
				Checking,03/02/2025,,Withdrawal,,,,-88.00
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(88.00m, transactions[0].Amount);
		}
	}
}
