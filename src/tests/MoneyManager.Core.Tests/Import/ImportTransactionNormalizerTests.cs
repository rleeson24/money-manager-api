using MoneyManager.Core.Import;
using MoneyManager.Core.Models;
using Xunit;

namespace MoneyManager.Core.Tests.Import;

public class ImportTransactionNormalizerTests
{
	private readonly ImportTransactionNormalizer _normalizer = new();

	[Fact]
	public void Normalize_DoesNotFlipSignForArvest()
	{
		var transaction = new BankTransaction { Amount = -25.50m };

		var result = _normalizer.Normalize(transaction, ImportSource.Arvest);

		Assert.Equal(-25.50m, result.Amount);
	}

	[Theory]
	[InlineData(ImportSource.DiscoverSavings)]
	[InlineData(ImportSource.DiscoverChecking)]
	[InlineData(ImportSource.AbfcuSavings)]
	[InlineData(ImportSource.AbfcuChecking)]
	public void Normalize_FlipsSignForConfiguredSources(ImportSource source)
	{
		var transaction = new BankTransaction { Amount = -25.50m };

		var result = _normalizer.Normalize(transaction, source);

		Assert.Equal(25.50m, result.Amount);
	}
}
