using MoneyManager.Core.Constants;

namespace MoneyManager.Data
{
	/// <summary>
	/// Data layer configuration. Binds to "Data" configuration section.
	/// </summary>
	public class DataOptions
	{
		/// <summary>
		/// Obsolete: use <see cref="UseInMemoryDatabase"/> for local development without SQL Server.
		/// </summary>
		[Obsolete("Use Data:UseInMemoryDatabase for local dev. SQL repositories no longer honor UseMockData.")]
		public bool UseMockData { get; set; }

		/// <summary>
		/// When true, use an in-memory database (mutable, seeded from mock data). No SQL Server required.
		/// Ideal for development. Set "Data:UseInMemoryDatabase" to true in appsettings.Development.json.
		/// </summary>
		public bool UseInMemoryDatabase { get; set; }

		/// <summary>
		/// User ID assigned to expenses inserted by Aspire SQL bootstrap (see <c>Data:AspireSeedUserId</c>).
		/// List/filter APIs scope by authenticated user; use this GUID for local testing against seeded SQL data.
		/// </summary>
		public string AspireSeedUserId { get; set; } = AspireConstants.DefaultSeedUserId;

		/// <summary>
		/// Set to true by the Aspire AppHost so the API can detect orchestration without relying on DCP env vars alone.
		/// Do not set to true in appsettings when running the API outside Aspire unless you intend that behavior.
		/// </summary>
		public bool AspireOrchestrated { get; set; }
	}
}
