using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Utilities;

internal static class SqlConnectionStringHelper
{
	/// <summary>
	/// Tuning for SQL Server in Docker via Aspire: avoids TLS validation issues and allows longer TCP connect during container startup.
	/// </summary>
	public static string ApplyAspireSqlContainerDefaults(string connectionString)
	{
		var builder = new SqlConnectionStringBuilder(connectionString);
		builder.TrustServerCertificate = true;
		builder.ConnectTimeout = Math.Max(builder.ConnectTimeout, 60);
		return builder.ConnectionString;
	}
}
