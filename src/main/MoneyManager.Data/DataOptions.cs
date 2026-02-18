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
	}
}
