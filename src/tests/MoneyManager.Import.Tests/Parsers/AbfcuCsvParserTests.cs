using MoneyManager.Core.Models;
using MoneyManager.Import.Parsers;

namespace MoneyManager.Import.Tests.Parsers
{
	public class AbfcuCsvParserTests
	{
		private readonly AbfcuCsvParser _parser = new();

		[Fact]
		public void SourceKeys_ContainsAbfcuAccounts()
		{
			Assert.Contains("ABFCU Savings", _parser.SourceKeys);
			Assert.Contains("ABFCU Checking", _parser.SourceKeys);
		}

		[Fact]
		public async Task ParseAsync_SavingsFixture_ReturnsExpectedTransactions()
		{
			await using var stream = CsvFixtureHelper.OpenFixture("AbfcuSavings.csv");

			var transactions = await _parser.ParseAsync(stream);

			Assert.Equal(3, transactions.Count);

			Assert.Equal(new DateTime(2025, 2, 1), transactions[0].Date);
			Assert.Equal("Payroll Deposit", transactions[0].Description);
			Assert.Equal(2000.00m, transactions[0].Amount);
			Assert.Equal(BankAccountType.Depository, transactions[0].AccountType);

			Assert.Equal(new DateTime(2025, 2, 2), transactions[1].Date);
			Assert.Equal("ATM Withdrawal", transactions[1].Description);
			Assert.Equal(-100.00m, transactions[1].Amount);

			Assert.Equal(new DateTime(2025, 2, 3), transactions[2].Date);
			Assert.Equal("Transfer In", transactions[2].Description);
			Assert.Equal(25.00m, transactions[2].Amount);
		}

		[Fact]
		public async Task ParseAsync_CheckingFixture_ReturnsExpectedTransactions()
		{
			await using var stream = CsvFixtureHelper.OpenFixture("AbfcuChecking.csv");

			var transactions = await _parser.ParseAsync(stream);

			Assert.Equal(2, transactions.Count);
			Assert.Equal(350.00m, transactions[0].Amount);
			Assert.Equal(-75.50m, transactions[1].Amount);
		}

		[Fact]
		public async Task ParseAsync_InvalidHeader_ReturnsEmpty()
		{
			const string csv = "Account,Balance\n123,500.00\n";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Empty(transactions);
		}

		[Fact]
		public async Task ParseAsync_InvalidRowsSkipped()
		{
			const string csv = """
				Description,Category,Type,Date,Deposits,Withdrawals,Transaction Amount,Balance
				Bad Row,,Other,not-a-date,10.00,,,100.00
				Good Row,,Deposit,04/01/2025,15.00,,15.00,115.00
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal("Good Row", transactions[0].Description);
			Assert.Equal(15.00m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_UsesDepositsMinusWithdrawalsWhenTransactionAmountMissing()
		{
			const string csv = """
				Description,Category,Type,Date,Deposits,Withdrawals,Transaction Amount,Balance
				Split,,Transfer,05/01/2025,80.00,30.00,,1000.00
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(50.00m, transactions[0].Amount);
		}

		[Fact]
		public async Task ParseAsync_TransactionAmountInParentheses_IsNegative()
		{
			const string csv = """
				Description,Category,Type,Date,Deposits,Withdrawals,Transaction Amount,Balance
				Fee,,Withdrawal,05/02/2025,,,($12.34),987.66
				""";
			await using var stream = CsvFixtureHelper.ToStream(csv);

			var transactions = await _parser.ParseAsync(stream);

			Assert.Single(transactions);
			Assert.Equal(-12.34m, transactions[0].Amount);
		}
	}
}
