using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MoneyManager.Core.Import;
using MoneyManager.Core.Models;

namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// Parses OFX and QFX (Open Financial Exchange) transaction files.
	/// </summary>
	public sealed class OfxQfxParser : ITransactionFileParser
	{
		public async Task<IReadOnlyList<BankTransaction>> ParseAsync(Stream fileContent, string format, string? sourceKey = null, CancellationToken cancellationToken = default)
		{
			using var reader = new StreamReader(fileContent, Encoding.UTF8, leaveOpen: true);
			var text = await reader.ReadToEndAsync(cancellationToken);
			// OFX can have SGML-style headers; normalize to XML for parsing
			text = NormalizeOfxToXml(text);
			var doc = XDocument.Parse(text);
			var transactions = new List<BankTransaction>();
			var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
			// OFX 2.x: BANKMSGSRSV1 -> STMTTRNRS -> BANKTRANLIST -> STMTTRN
			var stmtTrns = doc.Descendants(ns + "STMTTRN").ToList();
			if (stmtTrns.Count == 0)
				stmtTrns = doc.Descendants().Where(e => e.Name.LocalName == "STMTTRN").ToList();
			BankAccountType accountType = BankAccountType.Depository;
			var acctTypeEl = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "ACCTTYPE");
			if (acctTypeEl != null)
			{
				var v = acctTypeEl.Value.ToUpperInvariant();
				if (v.Contains("CREDIT") || v == "CC")
					accountType = BankAccountType.CreditCard;
			}
			foreach (var stmt in stmtTrns)
			{
				var dateStr = GetElementValue(stmt, "DTPOSTED") ?? GetElementValue(stmt, "DTUSER");
				if (string.IsNullOrEmpty(dateStr)) continue;
				if (!TryParseOfxDate(dateStr, out var date)) continue;
				var amtStr = GetElementValue(stmt, "TRNAMT");
				if (string.IsNullOrEmpty(amtStr) || !decimal.TryParse(amtStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
					continue;
				var name = GetElementValue(stmt, "NAME") ?? GetElementValue(stmt, "MEMO") ?? "";
				var memo = GetElementValue(stmt, "MEMO");
				var description = string.IsNullOrEmpty(memo) ? name : (name + " " + memo).Trim();
				var trnType = GetElementValue(stmt, "TRNTYPE")?.ToUpperInvariant();
				// Some banks use TRNTYPE (DEBIT/CREDIT) and unsigned TRNAMT; normalize to signed amount
				if (trnType == "DEBIT" && amount < 0) amount = -amount;
				else if (trnType == "CREDIT" && amount > 0) amount = -amount;
				transactions.Add(new BankTransaction
				{
					Date = date,
					Amount = amount,
					Description = description,
					AccountType = accountType
				});
			}
			return transactions;
		}

		private static string? GetElementValue(XElement parent, string localName)
		{
			var el = parent.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
			return el?.Value?.Trim();
		}

		private static bool TryParseOfxDate(string value, out DateTime date)
		{
			date = default;
			if (string.IsNullOrWhiteSpace(value)) return false;
			// OFX format: 20240115120000 or 20240115
			value = value.Trim();
			if (value.Length >= 8 && int.TryParse(value.AsSpan(0, 4), out var y) && int.TryParse(value.AsSpan(4, 2), out var m) && int.TryParse(value.AsSpan(6, 2), out var d))
			{
				try
				{
					date = new DateTime(y, m, d);
					return true;
				}
				catch { return false; }
			}
			return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
		}

		private static string NormalizeOfxToXml(string text)
		{
			// Remove OFX header (before first <)
			var firstBracket = text.IndexOf('<');
			if (firstBracket > 0)
				text = text.Substring(firstBracket);
			// Some OFX use unclosed tags; ensure we have a root wrapper if needed
			text = text.Trim();
			if (!text.StartsWith("<?xml") && !text.StartsWith("<OFX") && !text.StartsWith("<ofx"))
			{
				// Wrap in OFX root for fragment
				if (!text.Contains("<OFX") && !text.Contains("<ofx"))
					text = "<OFX>" + text + "</OFX>";
			}
			// Replace unclosed tags with self-closing for XML parser (e.g. <SOMETAG> with no </SOMETAG>)
			// Many OFX files have proper structure; if parsing fails we could try regex extraction
			return text;
		}
	}
}
