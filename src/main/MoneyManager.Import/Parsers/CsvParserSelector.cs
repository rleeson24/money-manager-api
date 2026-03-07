namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// Resolves the correct ICsvTransactionParser for a given source key.
	/// </summary>
	public interface ICsvParserSelector
	{
		ICsvTransactionParser? GetParser(string sourceKey);
	}

	public sealed class CsvParserSelector : ICsvParserSelector
	{
		private readonly IReadOnlyList<ICsvTransactionParser> _parsers;

		public CsvParserSelector(IEnumerable<ICsvTransactionParser> parsers)
		{
			_parsers = parsers.ToList();
		}

		public ICsvTransactionParser? GetParser(string sourceKey)
		{
			if (string.IsNullOrWhiteSpace(sourceKey)) return null;
			var key = sourceKey.Trim();
			foreach (var p in _parsers)
			{
				foreach (var k in p.SourceKeys)
				{
					if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
						return p;
				}
			}
			return null;
		}
	}
}
