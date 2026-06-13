using MoneyManager.Core.Constants;
using MoneyManager.Core.Models;
using MoneyManager.Import.Parsers;
using Moq;

namespace MoneyManager.Import.Tests
{
	public class CompositeTransactionFileParserTests
	{
		private static CompositeTransactionFileParser CreateParserWithAllParsers()
		{
			var selector = new CsvParserSelector([
				new ArvestCsvParser(),
				new AbfcuCsvParser(),
				new DiscoverBankCsvParser(),
				new DiscoverCreditCsvParser()
			]);
			return new CompositeTransactionFileParser(selector);
		}

		[Theory]
		[InlineData(ImportSource.Arvest, "Arvest.csv", 3)]
		[InlineData(ImportSource.AbfcuSavings, "AbfcuSavings.csv", 3)]
		[InlineData(ImportSource.AbfcuChecking, "AbfcuChecking.csv", 2)]
		[InlineData(ImportSource.DiscoverSavings, "DiscoverSavings.csv", 2)]
		[InlineData(ImportSource.DiscoverChecking, "DiscoverChecking.csv", 2)]
		[InlineData(ImportSource.DiscoverCredit, "DiscoverCredit.csv", 2)]
		public async Task ParseAsync_CsvWithImportSource_RoutesToCorrectParser(
			ImportSource importSource,
			string fixtureFile,
			int expectedCount)
		{
			var parser = CreateParserWithAllParsers();
			await using var stream = CsvFixtureHelper.OpenFixture(fixtureFile);

			var transactions = await parser.ParseAsync(stream, ImportFormat.Csv, importSource);

			Assert.Equal(expectedCount, transactions.Count);
		}

		[Fact]
		public async Task ParseAsync_CsvFormatCaseInsensitive()
		{
			var parser = CreateParserWithAllParsers();
			await using var stream = CsvFixtureHelper.OpenFixture("Arvest.csv");

			var transactions = await parser.ParseAsync(stream, "csv", ImportSource.Arvest);

			Assert.Equal(3, transactions.Count);
		}

		[Theory]
		[InlineData("JSON")]
		[InlineData("XML")]
		[InlineData("")]
		public async Task ParseAsync_UnsupportedFormat_ThrowsNotSupportedException(string format)
		{
			var parser = CreateParserWithAllParsers();
			await using var stream = CsvFixtureHelper.OpenFixture("Arvest.csv");

			await Assert.ThrowsAsync<NotSupportedException>(() =>
				parser.ParseAsync(stream, format, ImportSource.Arvest));
		}

		[Fact]
		public async Task ParseAsync_MissingImportSource_ThrowsInvalidOperationException()
		{
			var parser = CreateParserWithAllParsers();
			await using var stream = CsvFixtureHelper.OpenFixture("Arvest.csv");

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				parser.ParseAsync(stream, ImportFormat.Csv, importSource: null));
		}

		[Fact]
		public async Task ParseAsync_NoParserRegistered_ThrowsNotSupportedException()
		{
			var selector = new Mock<ICsvParserSelector>();
			selector.Setup(s => s.GetParser(It.IsAny<string>())).Returns((ICsvTransactionParser?)null);
			var parser = new CompositeTransactionFileParser(selector.Object);
			await using var stream = CsvFixtureHelper.OpenFixture("Arvest.csv");

			var ex = await Assert.ThrowsAsync<NotSupportedException>(() =>
				parser.ParseAsync(stream, ImportFormat.Csv, ImportSource.Arvest));

			Assert.Contains("Arvest", ex.Message);
		}

		[Fact]
		public async Task ParseAsync_DelegatesToSelectedParser()
		{
			var expected = new List<BankTransaction>
			{
				new()
				{
					Date = new DateTime(2025, 1, 1),
					Amount = -10m,
					Description = "Test",
					AccountType = BankAccountType.Depository
				}
			};

			var csvParser = new Mock<ICsvTransactionParser>();
			csvParser.Setup(p => p.ParseAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(expected);

			var selector = new Mock<ICsvParserSelector>();
			selector.Setup(s => s.GetParser("Discover Credit")).Returns(csvParser.Object);

			var parser = new CompositeTransactionFileParser(selector.Object);
			await using var stream = CsvFixtureHelper.ToStream("ignored");

			var transactions = await parser.ParseAsync(stream, ImportFormat.Csv, ImportSource.DiscoverCredit);

			Assert.Same(expected, transactions);
			selector.Verify(s => s.GetParser("Discover Credit"), Times.Once);
			csvParser.Verify(p => p.ParseAsync(stream, It.IsAny<CancellationToken>()), Times.Once);
		}
	}
}
