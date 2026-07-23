using System.Globalization;
using System.Linq;
using System.Text;
using MoneyManager.Core.Models;

namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// Common CSV parsing helpers for account-specific parsers. Override ParseRow to implement layout.
	/// </summary>
	public abstract class CsvTransactionParserBase : ICsvTransactionParser
	{
		public abstract IReadOnlyList<string> SourceKeys { get; }

		public virtual async Task<IReadOnlyList<BankTransaction>> ParseAsync(Stream fileContent, CancellationToken cancellationToken = default)
		{
			using var reader = new StreamReader(fileContent, Encoding.UTF8, leaveOpen: true);
			var headerLine = await reader.ReadLineAsync(cancellationToken);
			if (string.IsNullOrWhiteSpace(headerLine))
				return Array.Empty<BankTransaction>();
			var headers = ParseCsvRow(headerLine);
			if (!ValidateHeader(headers))
				return Array.Empty<BankTransaction>();

			var list = new List<BankTransaction>();
			string? line;
			while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
			{
				if (string.IsNullOrWhiteSpace(line)) continue;
				var cols = ParseCsvRow(line);
				if (TryParseRow(headers, cols, out var transaction))
					list.Add(transaction);
			}
			return list;
		}

		/// <summary>
		/// Validate that the header row contains expected columns. Return false to reject the file.
		/// </summary>
		protected abstract bool ValidateHeader(IReadOnlyList<string> headers);

		/// <summary>
		/// Parse one data row into a transaction. Return false to skip the row.
		/// </summary>
		protected abstract bool TryParseRow(IReadOnlyList<string> headers, IReadOnlyList<string> cols, out BankTransaction transaction);

		protected static IReadOnlyList<string> ParseCsvRow(string line)
		{
			var list = new List<string>();
			var sb = new StringBuilder();
			var inQuotes = false;
			for (var i = 0; i < line.Length; i++)
			{
				var c = line[i];
				if (c == '"')
				{
					inQuotes = !inQuotes;
					continue;
				}
				if (!inQuotes && (c == ',' || c == '\t'))
				{
					list.Add(sb.ToString().Trim());
					sb.Clear();
					continue;
				}
				sb.Append(c);
			}
			list.Add(sb.ToString().Trim());
			return list;
		}

		protected static int FindColumn(IReadOnlyList<string> headers, params string[] names)
		{
			var normalized = headers.Select(h => h.Trim().Replace(" ", "").Replace(".", "").ToUpperInvariant()).ToList();
			foreach (var name in names)
			{
				var key = name.Replace(" ", "").Replace(".", "");
				for (var i = 0; i < normalized.Count; i++)
				{
					if (string.Equals(normalized[i].Replace(" ", "").Replace(".", ""), key, StringComparison.OrdinalIgnoreCase))
						return i;
				}
			}
			return -1;
		}

		protected static bool TryParseDate(string value, out DateTime date)
		{
			date = default;
			if (string.IsNullOrWhiteSpace(value)) return false;
			value = value.Trim();
			var formats = new[] { "MM/dd/yyyy", "M/d/yyyy", "MM/dd/yyyy HH:mm:ss", "M/d/yyyy H:mm:ss", "yyyy-MM-dd" };
			foreach (var fmt in formats)
			{
				if (DateTime.TryParseExact(value, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
					return true;
			}
			return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
		}

		/// <summary>
		/// Parse amount from strings like "56.00", "$1,234.56", "($20.00)", "-447.44".
		/// </summary>
		protected static bool TryParseAmount(string value, out decimal amount)
		{
			amount = 0;
			if (string.IsNullOrWhiteSpace(value)) return false;
			value = value.Trim();
			var negative = value.Contains('(') && value.Contains(')');
			value = value.Replace("$", "").Replace(",", "").Replace("(", "").Replace(")", "").Trim();
			if (string.IsNullOrEmpty(value)) return false;
			if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
				return false;
			if (negative) amount = -Math.Abs(amount);
			return true;
		}
	}
}
