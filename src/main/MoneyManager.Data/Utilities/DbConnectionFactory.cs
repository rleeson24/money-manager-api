using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Utilities
{
	public class DbConnectionFactory
	{
		private readonly string connectionString;

		public DbConnectionFactory(string connectionString)
		{
			this.connectionString = connectionString;
		}

		public SqlConnection CreateConnection()
		{
			return new SqlConnection(connectionString);
		}
	}
}
