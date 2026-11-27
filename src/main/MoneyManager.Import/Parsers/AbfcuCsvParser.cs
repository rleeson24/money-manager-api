using MoneyManager.Core.Models;

namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// ABFCU Savings and Checking CSV.
	/// Current savings export: Account, Date, Description, Note, Check #, Amount, Balance.
	/// Descriptions may span lines inside quotes. Withdrawals are parenthesized, e.g. ($20.00).
	/// Older exports: Description, Category, Type, Date, Deposits, Withdrawals, Transaction Amount, Balance.
	/// </summary>
	public sealed class AbfcuCsvParser : CsvTransactionParserBase
	{
		public override IReadOnlyList<string> SourceKeys { get; } = new[] { "ABFCU Savings", "ABFCU Checking" };

		protected override bool ValidateHeader(IReadOnlyList<string> headers)
		{
			var dateIdx = FindColumn(headers, "Date");
			var depositsIdx = FindColumn(headers, "Deposits");
			var withdrawalsIdx = FindColumn(headers, "Withdrawals");
			var txnAmtIdx = FindAmountColumn(headers);
			return dateIdx >= 0 && (depositsIdx >= 0 || withdrawalsIdx >= 0 || txnAmtIdx >= 0);
		}

		protected override bool TryParseRow(IReadOnlyList<string> headers, IReadOnlyList<string> cols, out BankTransaction transaction)
		{
			transaction = null!;
			var dateIdx = FindColumn(headers, "Date");
			var descIdx = FindColumn(headers, "Description");
			var depositsIdx = FindColumn(headers, "Deposits");
			var withdrawalsIdx = FindColumn(headers, "Withdrawals");
			var txnAmtIdx = FindAmountColumn(headers);
			if (dateIdx < 0 || dateIdx >= cols.Count)
				return false;
			if (!TryParseDate(cols[dateIdx], out var date))
				return false;

			decimal amount;
			if (txnAmtIdx >= 0 && txnAmtIdx < cols.Count && TryParseAmount(cols[txnAmtIdx], out amount))
			{
				// Transaction Amount or Amount, including parenthesized withdrawals such as ($20.00).
			}
			else
			{
				decimal deposits = 0, withdrawals = 0;
				if (depositsIdx >= 0 && depositsIdx < cols.Count)
					TryParseAmount(cols[depositsIdx], out deposits);
				if (withdrawalsIdx >= 0 && withdrawalsIdx < cols.Count)
					TryParseAmount(cols[withdrawalsIdx], out withdrawals);
				amount = deposits - withdrawals;
			}

			var description = descIdx >= 0 && descIdx < cols.Count ? cols[descIdx] : "";
			transaction = new BankTransaction
			{
				Date = date.Date,
				Amount = amount,
				Description = NormalizeDescription(description),
				AccountType = BankAccountType.Depository
			};
			return true;
		}

		/// <summary>
		/// Prefer the legacy Transaction Amount column so it is not confused with a shorter Amount header.
		/// </summary>
		private static int FindAmountColumn(IReadOnlyList<string> headers)
		{
			var transactionAmountIdx = FindColumn(headers, "Transaction Amount");
			return transactionAmountIdx >= 0 ? transactionAmountIdx : FindColumn(headers, "Amount");
		}

		private static string NormalizeDescription(string? description)
		{
			if (string.IsNullOrWhiteSpace(description))
				return "";

			var parts = description.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
			return string.Join(" ", parts);
		}
	}
}
