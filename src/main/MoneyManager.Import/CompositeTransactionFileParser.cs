using MoneyManager.Core.Constants;
using MoneyManager.Core.Import;
using MoneyManager.Core.Models;

namespace MoneyManager.Import
{
	/// <summary>
	/// Routes CSV files to account-specific parsers based on importSource.
	/// </summary>
	public sealed class CompositeTransactionFileParser : ITransactionFileParser
	{
		private readonly ICsvParserSelector _csvParserSelector;

		public CompositeTransactionFileParser(ICsvParserSelector csvParserSelector)
		{
			_csvParserSelector = csvParserSelector;
		}

		public async Task<IReadOnlyList<BankTransaction>> ParseAsync(Stream fileContent, string format, ImportSource? importSource = null, CancellationToken cancellationToken = default)
		{
			var fmt = format?.Trim().ToUpperInvariant() ?? "";
			if (!ImportFormat.IsCsv(fmt))
				throw new NotSupportedException($"Format '{format}' is not supported. Use {ImportFormat.Csv}.");

			if (!importSource.HasValue)
				throw new InvalidOperationException("Import source is required for CSV import.");

			var sourceKey = importSource.Value.ToSourceKey();
			var csvParser = _csvParserSelector.GetParser(sourceKey);
			if (csvParser == null)
				throw new NotSupportedException($"No CSV parser registered for source '{sourceKey}'.");

			return await csvParser.ParseAsync(fileContent, cancellationToken);
		}
	}
}
