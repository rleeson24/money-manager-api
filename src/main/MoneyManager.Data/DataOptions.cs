namespace MoneyManager.Data
{
	/// <summary>
	/// Data layer configuration. Binds to "Data" configuration section.
	/// </summary>
	public class DataOptions
	{
	/// <summary>
	/// When true, repositories return mock data instead of querying the database.
	/// Set via configuration key "Data:UseMockData" or environment variable "Data__UseMockData".
	/// </summary>
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
	public string AspireSeedUserId { get; set; } = "11111111-1111-1111-1111-111111111111";

	/// <summary>
	/// Set to true by the Aspire AppHost so the API can detect orchestration without relying on DCP env vars alone.
	/// Do not set to true in appsettings when running the API outside Aspire unless you intend that behavior.
	/// </summary>
	public bool AspireOrchestrated { get; set; }
	}
}
