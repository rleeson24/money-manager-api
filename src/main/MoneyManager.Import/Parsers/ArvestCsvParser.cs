using MoneyManager.Core.Models;

namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// Arvest CSV: Account, Date, Pending?, Description, Category, Check, Credit, Debit
	/// Credit and Debit columns; Debit is often exported as negative.
	/// </summary>
	public sealed class ArvestCsvParser : CsvTransactionParserBase
	{
		public override IReadOnlyList<string> SourceKeys { get; } = new[] { "Arvest" };

		protected override bool ValidateHeader(IReadOnlyList<string> headers)
		{
			var dateIdx = FindColumn(headers, "Date");
			var creditIdx = FindColumn(headers, "Credit");
			var debitIdx = FindColumn(headers, "Debit");
			return dateIdx >= 0 && (creditIdx >= 0 || debitIdx >= 0);
		}

		protected override bool TryParseRow(IReadOnlyList<string> headers, IReadOnlyList<string> cols, out BankTransaction transaction)
		{
			transaction = null!;
			var dateIdx = FindColumn(headers, "Date");
			var descIdx = FindColumn(headers, "Description");
			var creditIdx = FindColumn(headers, "Credit");
			var debitIdx = FindColumn(headers, "Debit");
			if (dateIdx < 0 || dateIdx >= cols.Count)
				return false;
			if (!TryParseDate(cols[dateIdx], out var date))
				return false;

			decimal credit = 0, debit = 0;
			if (creditIdx >= 0 && creditIdx < cols.Count)
				TryParseAmount(cols[creditIdx], out credit);
			if (debitIdx >= 0 && debitIdx < cols.Count)
				TryParseAmount(cols[debitIdx], out debit);

			// Arvest exports Debit as negative; Credit as positive. Net = credit + debit.
			var amount = -Math.Abs(credit) + Math.Abs(debit);
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
