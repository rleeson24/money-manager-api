namespace MoneyManager.Core.Import
{
	/// <summary>
	/// Parses OFX, QFX, or CSV transaction files into a list of bank transactions.
	/// </summary>
	public interface ITransactionFileParser
	{
	/// <summary>
	/// Parse file content. Format is "OFX", "QFX", or "CSV".
	/// When format is CSV, sourceKey is used to select the account-specific parser (e.g. "Arvest", "Discover Credit").
	/// </summary>
	Task<IReadOnlyList<Models.BankTransaction>> ParseAsync(Stream fileContent, string format, string? sourceKey = null, CancellationToken cancellationToken = default);
	}
}
