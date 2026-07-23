using System.Text;
using MoneyManager.Core.Models;

namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// Arvest CSV exports include an account summary row, then columns:
	/// Date, Account, Description, Check #, Category, Credit, Debit.
	/// Legacy exports used Account, Date, Pending?, Description, Category, Check, Credit, Debit.
	/// Credit and Debit columns; Debit is often exported as negative.
	/// </summary>
	public sealed class ArvestCsvParser : CsvTransactionParserBase
	{
		public override IReadOnlyList<string> SourceKeys { get; } = new[] { "Arvest" };

		public override async Task<IReadOnlyList<BankTransaction>> ParseAsync(
			Stream fileContent,
			CancellationToken cancellationToken = default)
		{
			using var reader = new StreamReader(fileContent, Encoding.UTF8, leaveOpen: true);
			IReadOnlyList<string>? headers = null;
			string? line;
			while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
			{
				if (string.IsNullOrWhiteSpace(line))
					continue;

				var candidate = ParseCsvRow(line);
				if (ValidateHeader(candidate))
				{
					headers = candidate;
					break;
				}
			}

			if (headers == null)
				return Array.Empty<BankTransaction>();

			var list = new List<BankTransaction>();
			while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
			{
				if (string.IsNullOrWhiteSpace(line))
					continue;

				var cols = ParseCsvRow(line);
				if (TryParseRow(headers, cols, out var transaction))
					list.Add(transaction);
			}

			return list;
		}

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
			if (IsFooterOrSummaryRow(cols))
				return false;

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

		private static bool IsFooterOrSummaryRow(IReadOnlyList<string> cols)
		{
			foreach (var col in cols)
			{
				if (string.IsNullOrWhiteSpace(col))
					continue;

				var normalized = col.Trim().TrimEnd(':');
				if (normalized.Equals("Totals", StringComparison.OrdinalIgnoreCase))
					return true;

				if (normalized.EndsWith(" items", StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}
	}
}
