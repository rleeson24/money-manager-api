using MoneyManager.Core.Models;

namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// Discover Savings and Checking CSV: Transaction Date, Transaction Description, Transaction Type, Debit, Credit, Balance
	/// Debit and Credit are separate columns (positive numbers); debit = money out.
	/// </summary>
	public sealed class DiscoverBankCsvParser : CsvTransactionParserBase
	{
		public override IReadOnlyList<string> SourceKeys { get; } = new[] { "Discover Savings", "Discover Checking" };

		protected override bool ValidateHeader(IReadOnlyList<string> headers)
		{
			var dateIdx = FindColumn(headers, "Transaction Date");
			var debitIdx = FindColumn(headers, "Debit");
			var creditIdx = FindColumn(headers, "Credit");
			return dateIdx >= 0 && (debitIdx >= 0 || creditIdx >= 0);
		}

		protected override bool TryParseRow(IReadOnlyList<string> headers, IReadOnlyList<string> cols, out BankTransaction transaction)
		{
			transaction = null!;
			var dateIdx = FindColumn(headers, "Transaction Date");
			var descIdx = FindColumn(headers, "Transaction Description");
			var debitIdx = FindColumn(headers, "Debit");
			var creditIdx = FindColumn(headers, "Credit");
			if (dateIdx < 0 || dateIdx >= cols.Count)
				return false;
			if (!TryParseDate(cols[dateIdx], out var date))
				return false;

			decimal debit = 0, credit = 0;
			if (debitIdx >= 0 && debitIdx < cols.Count)
				TryParseAmount(cols[debitIdx], out debit);
			if (creditIdx >= 0 && creditIdx < cols.Count)
				TryParseAmount(cols[creditIdx], out credit);

			// Debit and Credit are unsigned in the file; amount = credit - debit (debits negative)
			var amount = credit - debit;
			var description = descIdx >= 0 && descIdx < cols.Count ? cols[descIdx] : "";
			transaction = new BankTransaction
			{
				Date = date.Date,
				Amount = amount,
				Description = description ?? "",
				AccountType = BankAccountType.Depository
			};
			return true;
		}
	}
}
