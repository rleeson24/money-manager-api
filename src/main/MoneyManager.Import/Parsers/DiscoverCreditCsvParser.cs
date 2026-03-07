using MoneyManager.Core.Models;

namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// Discover Credit CSV: Trans. Date, Post Date, Description, Amount, Category
	/// </summary>
	public sealed class DiscoverCreditCsvParser : CsvTransactionParserBase
	{
		public override IReadOnlyList<string> SourceKeys { get; } = new[] { "Discover Credit" };

		protected override bool ValidateHeader(IReadOnlyList<string> headers)
		{
			var amountIdx = FindColumn(headers, "Amount");
			var dateIdx = FindColumn(headers, "Trans. Date", "Post Date");
			return amountIdx >= 0 && dateIdx >= 0;
		}

		protected override bool TryParseRow(IReadOnlyList<string> headers, IReadOnlyList<string> cols, out BankTransaction transaction)
		{
			transaction = null!;
			var dateIdx = FindColumn(headers, "Trans. Date", "Post Date");
			var descIdx = FindColumn(headers, "Description");
			var amountIdx = FindColumn(headers, "Amount");
			if (dateIdx < 0 || amountIdx < 0 || dateIdx >= cols.Count || amountIdx >= cols.Count)
				return false;
			if (!TryParseDate(cols[dateIdx], out var date) || !TryParseAmount(cols[amountIdx], out var amount))
				return false;
			var description = descIdx >= 0 && descIdx < cols.Count ? cols[descIdx] : "";
			transaction = new BankTransaction
			{
				Date = date.Date,
				Amount = amount,
				Description = description ?? "",
				AccountType = BankAccountType.CreditCard
			};
			return true;
		}
	}
}
