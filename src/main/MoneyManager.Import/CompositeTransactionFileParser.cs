using MoneyManager.Core.Import;
using MoneyManager.Core.Models;
using MoneyManager.Import.Parsers;

namespace MoneyManager.Import
{
	/// <summary>
	/// Routes to OFX/QFX or CSV parser based on format. For CSV, uses importSource to select account-specific parser.
	/// </summary>
	public sealed class CompositeTransactionFileParser : ITransactionFileParser
	{
		private readonly OfxQfxParser _ofxParser;
		private readonly ICsvParserSelector _csvParserSelector;

		public CompositeTransactionFileParser(OfxQfxParser ofxParser, ICsvParserSelector csvParserSelector)
		{
			_ofxParser = ofxParser;
			_csvParserSelector = csvParserSelector;
		}

		public async Task<IReadOnlyList<BankTransaction>> ParseAsync(Stream fileContent, string format, ImportSource? importSource = null, CancellationToken cancellationToken = default)
		{
			var fmt = format?.Trim().ToUpperInvariant() ?? "";
			if (fmt == "CSV")
			{
				if (!importSource.HasValue)
					throw new InvalidOperationException("Import source is required for CSV import.");
				var sourceKey = importSource.Value.ToSourceKey();
				var csvParser = _csvParserSelector.GetParser(sourceKey);
				if (csvParser == null)
					throw new NotSupportedException($"No CSV parser registered for source '{sourceKey}'.");
				return await csvParser.ParseAsync(fileContent, cancellationToken);
			}
			if (fmt == "OFX" || fmt == "QFX")
				return await _ofxParser.ParseAsync(fileContent, format, null, cancellationToken);
			throw new NotSupportedException($"Format '{format}' is not supported. Use OFX, QFX, or CSV.");
		}
	}
}
