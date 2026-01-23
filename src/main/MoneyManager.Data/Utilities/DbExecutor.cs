using Microsoft.Data.SqlClient;
using System.Data;

namespace MoneyManager.Data.Utilities
{
	public class DbExecutor
	{
		private readonly DbConnectionFactory _connectionFactory;

		public DbExecutor(DbConnectionFactory connectionFactory)
		{
			_connectionFactory = connectionFactory;
		}

		public async Task Execute(CommandType commandType, string commandText, IEnumerable<SqlParameter> parameters, Func<SqlCommand, Task> proc)
		{
			using (var connection = _connectionFactory.CreateConnection())
				await Execute(commandType, commandText, connection, parameters, proc);
		}

		public async Task Execute(CommandType commandType, string commandText, IEnumerable<SqlParameter> parameters)
		{
			using (var connection = _connectionFactory.CreateConnection())
				await Execute(commandType, commandText, connection, parameters);
		}

		public async Task Execute(string commandText, IEnumerable<SqlParameter> parameters, Func<SqlCommand, Task> proc)
		{
			using (var connection = _connectionFactory.CreateConnection())
				await Execute(CommandType.Text, commandText, connection, parameters, proc);
		}

		public async Task Execute(string commandText, IEnumerable<SqlParameter> parameters)
		{
			using (var connection = _connectionFactory.CreateConnection())
				await Execute(CommandType.Text, commandText, connection, parameters);
		}

		public async Task Execute(string commandText, Func<SqlCommand, Task> proc)
		{
			using (var connection = _connectionFactory.CreateConnection())
				await Execute(CommandType.Text, commandText, connection, Array.Empty<SqlParameter>(), proc);
		}

		public async Task<object?> ExecuteScalar(string commandText, IEnumerable<SqlParameter> parameters)
		{
			var result = default(object?);
			using (var connection = _connectionFactory.CreateConnection())
			{
				await Execute(CommandType.Text, commandText, connection, parameters,
					async cmd =>
					{
						result = await cmd.ExecuteScalarAsync();
					}
				);
			}
			return result;
		}

		public async Task<object?> ExecuteScalar(string commandText)
		{
			return await ExecuteScalar(commandText, Array.Empty<SqlParameter>());
		}

		public async Task<int> ExecuteNonQuery(string commandText, IEnumerable<SqlParameter> parameters)
		{
			var rowsChanged = 0;
			using (var connection = _connectionFactory.CreateConnection())
			{
				await Execute(CommandType.Text, commandText, connection, parameters,
					async cmd =>
					{
						rowsChanged = await cmd.ExecuteNonQueryAsync();
					}
				);
			}
			return rowsChanged;
		}

		public async Task<int> ExecuteNonQuery(string commandText)
		{
			return await ExecuteNonQuery(commandText, Array.Empty<SqlParameter>());
		}

		public async Task ExecuteReader(string commandText, IEnumerable<SqlParameter> parameters, Func<SqlDataReader, Task> proc)
		{
			using (var connection = _connectionFactory.CreateConnection())
			{
				await Execute(CommandType.Text, commandText, connection, parameters,
					async dbCommand =>
					{
						using (var reader = await dbCommand.ExecuteReaderAsync())
						{
							await proc(reader);
						}
					}
				);
			}
		}

		public async Task Execute(CommandType commandType, string commandText, SqlConnection connection, IEnumerable<SqlParameter> parameters, Func<SqlCommand, Task>? proc)
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = commandText;
				command.CommandType = commandType;
				command.Parameters.AddRange(parameters.ToArray());
				if (connection.State != System.Data.ConnectionState.Open)
				{
					connection.Open();
				}
				if (proc != null)
				{
					await proc(command);
				}
			}
		}

		public async Task Execute(CommandType commandType, string commandText, SqlConnection connection, IEnumerable<SqlParameter> parameters)
		{
			await Execute(commandType, commandText, connection, parameters, null);
		}
	}
}
