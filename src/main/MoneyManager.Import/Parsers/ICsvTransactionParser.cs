using MoneyManager.Core.Models;

namespace MoneyManager.Import.Parsers
{
	/// <summary>
	/// Parses CSV transaction files for a specific account/source. One implementation per account type.
	/// </summary>
	public interface ICsvTransactionParser
	{
		/// <summary>
		/// Source keys this parser handles (e.g. "Arvest", "Discover Credit"). Used to select parser when importing CSV.
		/// </summary>
		IReadOnlyList<string> SourceKeys { get; }

		Task<IReadOnlyList<BankTransaction>> ParseAsync(Stream fileContent, CancellationToken cancellationToken = default);
	}
}
