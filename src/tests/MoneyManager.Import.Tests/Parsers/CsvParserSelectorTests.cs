using MoneyManager.Import.Parsers;

namespace MoneyManager.Import.Tests.Parsers
{
	public class CsvParserSelectorTests
	{
		private readonly CsvParserSelector _selector = new([
			new ArvestCsvParser(),
			new AbfcuCsvParser(),
			new DiscoverBankCsvParser(),
			new DiscoverCreditCsvParser()
		]);

		[Theory]
		[InlineData("Arvest", typeof(ArvestCsvParser))]
		[InlineData("arvest", typeof(ArvestCsvParser))]
		[InlineData("ABFCU Savings", typeof(AbfcuCsvParser))]
		[InlineData("abfcu checking", typeof(AbfcuCsvParser))]
		[InlineData("Discover Savings", typeof(DiscoverBankCsvParser))]
		[InlineData("Discover Checking", typeof(DiscoverBankCsvParser))]
		[InlineData("Discover Credit", typeof(DiscoverCreditCsvParser))]
		public void GetParser_KnownSourceKey_ReturnsExpectedParser(string sourceKey, Type expectedType)
		{
			var parser = _selector.GetParser(sourceKey);

			Assert.NotNull(parser);
			Assert.IsType(expectedType, parser);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		[InlineData("Chase")]
		public void GetParser_UnknownOrEmptySourceKey_ReturnsNull(string? sourceKey)
		{
			var parser = _selector.GetParser(sourceKey!);

			Assert.Null(parser);
		}

		[Fact]
		public void GetParser_TrimsSourceKey()
		{
			var parser = _selector.GetParser("  Arvest  ");

			Assert.NotNull(parser);
			Assert.IsType<ArvestCsvParser>(parser);
		}
	}
}
