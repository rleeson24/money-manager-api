using MoneyManager.Core.Models;

namespace MoneyManager.Core.Import
{
	/// <summary>
	/// Parses OFX, QFX, or CSV transaction files into a list of bank transactions.
	/// </summary>
	public interface ITransactionFileParser
	{
		/// <summary>
		/// Parse file content. Format is "OFX", "QFX", or "CSV".
		/// When format is CSV, importSource selects the account-specific parser.
		/// </summary>
		Task<IReadOnlyList<BankTransaction>> ParseAsync(Stream fileContent, string format, ImportSource? importSource = null, CancellationToken cancellationToken = default);
	}
}
