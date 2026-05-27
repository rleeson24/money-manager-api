using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using MoneyManager.Core.Models;
using MoneyManager.Data.Repositories;
using MoneyManager.Data.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoneyManager.Data.Bootstrap;

/// <summary>
/// Under .NET Aspire with SQL Server: applies DDL schema (CreateTables.sql), seeds catalog rows from
/// <see cref="LegacyCategorySeed"/> / <see cref="LegacyPaymentMethodSeed"/>, and inserts
/// <see cref="MockData"/> expenses once per seed user when the database is empty for that user.
/// </summary>
internal sealed class AspireSqlDevelopmentBootstrap : IHostedService
{
	private const string SchemaResourceName = "MoneyManager.Data.Resources.CreateTables.sql";

	private readonly DbConnectionFactory _connectionFactory;
	private readonly DbExecutor _db;
	private readonly DataOptions _options;
	private readonly ILogger<AspireSqlDevelopmentBootstrap> _logger;

	public AspireSqlDevelopmentBootstrap(
		DbConnectionFactory connectionFactory,
		DbExecutor db,
		IOptions<DataOptions> dataOptions,
		ILogger<AspireSqlDevelopmentBootstrap> logger)
	{
		_connectionFactory = connectionFactory;
		_db = db;
		_options = dataOptions.Value;
		_logger = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		if (!Guid.TryParse(_options.AspireSeedUserId, out var seedUserId))
		{
			_logger.LogWarning(
				"Aspire SQL bootstrap skipped: invalid Data:AspireSeedUserId '{Value}'",
				_options.AspireSeedUserId);
			return;
		}

		_logger.LogInformation("Aspire SQL bootstrap starting for seed user {UserId}.", seedUserId);

		const int maxAttempts = 30;
		Exception? lastException = null;
		for (var attempt = 1; attempt <= maxAttempts; attempt++)
		{
			try
			{
				await RunBootstrapCoreAsync(seedUserId, cancellationToken).ConfigureAwait(false);
				return;
			}
			catch (Exception ex) when (attempt < maxAttempts && IsSqlConnectivityTransient(ex))
			{
				lastException = ex;
				_logger.LogWarning(
					ex,
					"Aspire SQL bootstrap attempt {Attempt}/{Max} failed (SQL may still be starting); retrying in 2s.",
					attempt,
					maxAttempts);
				await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Aspire SQL bootstrap failed on attempt {Attempt}/{Max}.", attempt, maxAttempts);
				throw;
			}
		}

		_logger.LogError(lastException, "Aspire SQL bootstrap failed after {Max} attempts.", maxAttempts);
		throw lastException ?? new InvalidOperationException("Aspire SQL bootstrap failed after all retry attempts.");
	}

	private async Task RunBootstrapCoreAsync(Guid seedUserId, CancellationToken cancellationToken)
	{
		var schemaSql = await LoadSchemaSqlAsync(cancellationToken).ConfigureAwait(false);
		await ApplySchemaAsync(schemaSql, cancellationToken).ConfigureAwait(false);
		await SeedCatalogAsync(cancellationToken).ConfigureAwait(false);

		var categoryMap = await BuildCategoryMapAsync(cancellationToken).ConfigureAwait(false);
		var paymentMethodMap = await BuildPaymentMethodMapAsync(cancellationToken).ConfigureAwait(false);

		var expenseCountObj = await _db.ExecuteScalar(
			"SELECT COUNT(*) FROM dbo.Expenses WHERE UserId = @UserId",
			[new SqlParameter("@UserId", seedUserId)]).ConfigureAwait(false);
		var expenseCount = expenseCountObj != null ? Convert.ToInt32(expenseCountObj) : 0;
		if (expenseCount > 0)
		{
			_logger.LogInformation(
				"Aspire SQL bootstrap: database already has {Count} expense(s) for seed user {UserId}; skipping mock inserts.",
				expenseCount,
				seedUserId);
			return;
		}

		var expenseKeyMap = new Dictionary<int, int>();
		foreach (var e in MockData.Expenses)
		{
			var dbExpenseId = await InsertExpenseAsync(e, seedUserId, categoryMap, paymentMethodMap, cancellationToken)
				.ConfigureAwait(false);
			expenseKeyMap[e.Expense_I] = dbExpenseId;
		}

		foreach (var split in MockData.ExpenseSplits)
		{
			if (!expenseKeyMap.TryGetValue(split.Expense_I, out var dbExpenseId))
			{
				_logger.LogWarning("Aspire SQL bootstrap: skipping split {SplitId}; parent expense {Expense_I} not mapped.", split.Id, split.Expense_I);
				continue;
			}

			var dbCategoryId = categoryMap[split.Category];
			await _db.ExecuteScalar(
				@"INSERT INTO dbo.Expenses_split (Expense_I, UserId, Description, Amount, Category, CreatedDateTime)
				  VALUES (@Expense_I, @UserId, @Description, @Amount, @Category, @CreatedDateTime);
				  SELECT CAST(SCOPE_IDENTITY() AS INT);",
				[
					new SqlParameter("@Expense_I", dbExpenseId),
					new SqlParameter("@UserId", seedUserId),
					new SqlParameter("@Description", split.Description),
					new SqlParameter("@Amount", split.Amount),
					new SqlParameter("@Category", dbCategoryId),
					new SqlParameter("@CreatedDateTime", split.CreatedDateTime == default ? DateTime.UtcNow : split.CreatedDateTime)
				]).ConfigureAwait(false);
		}

		_logger.LogInformation(
			"Aspire SQL bootstrap: inserted {ExpenseCount} expense(s) for seed user {UserId} (configure JWT / tests to match this user to see data).",
			expenseKeyMap.Count,
			seedUserId);
	}

	private static bool IsSqlConnectivityTransient(Exception ex)
	{
		for (var e = ex; e != null; e = e.InnerException)
		{
			switch (e)
			{
				case SqlException sql:
					foreach (SqlError err in sql.Errors)
					{
						if (err.Number is 10053 or 10054 or 10060 or -2 or 4060 or 40197 or 40501 or 40613 or 49918 or 49919)
							return true;
					}

					var msg = sql.Message;
					if (msg.Contains("pre-login", StringComparison.OrdinalIgnoreCase)
					    || msg.Contains("connection was closed", StringComparison.OrdinalIgnoreCase)
					    || msg.Contains("connection was forcibly closed", StringComparison.OrdinalIgnoreCase)
					    || msg.Contains("transport-level", StringComparison.OrdinalIgnoreCase)
					    || msg.Contains("handshake", StringComparison.OrdinalIgnoreCase))
						return true;
					break;
				case Win32Exception w32 when w32.NativeErrorCode is 10053 or 10054:
					return true;
				case IOException:
					return true;
			}
		}
		return false;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	private async Task<string> LoadSchemaSqlAsync(CancellationToken cancellationToken)
	{
		var assembly = Assembly.GetExecutingAssembly();
		await using var stream = assembly.GetManifestResourceStream(SchemaResourceName);
		if (stream == null)
		{
			var names = string.Join(", ", assembly.GetManifestResourceNames());
			throw new InvalidOperationException($"Missing embedded resource '{SchemaResourceName}'. Known names: {names}");
		}

		using var reader = new StreamReader(stream);
		return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
	}

	private async Task ApplySchemaAsync(string schemaSql, CancellationToken cancellationToken)
	{
		var batches = SplitSqlBatches(schemaSql);
		await using var connection = _connectionFactory.CreateConnection();
		await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

		for (var i = 0; i < batches.Count; i++)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await using var command = connection.CreateCommand();
			command.CommandText = batches[i];
			command.CommandTimeout = 120;
			await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
		}

		_logger.LogInformation("Aspire SQL bootstrap: applied schema script ({BatchCount} batch(es)).", batches.Count);

		var tableCountObj = await ExecuteScalarOnConnectionAsync(
			connection,
			"SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')",
			cancellationToken).ConfigureAwait(false);
		var tableCount = tableCountObj != null ? Convert.ToInt32(tableCountObj) : 0;
		if (tableCount == 0)
		{
			throw new InvalidOperationException(
				"Aspire SQL bootstrap applied the schema script but no dbo tables were created.");
		}

		_logger.LogInformation("Aspire SQL bootstrap: verified {TableCount} dbo table(s) exist.", tableCount);
	}

	private async Task SeedCatalogAsync(CancellationToken cancellationToken)
	{
		await using var connection = _connectionFactory.CreateConnection();
		await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

		await ExecuteNonQueryOnConnectionAsync(connection, "SET IDENTITY_INSERT dbo.Categories ON;", cancellationToken)
			.ConfigureAwait(false);
		try
		{
			foreach (var category in LegacyCategorySeed.Categories)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await ExecuteNonQueryOnConnectionAsync(
					connection,
					@"MERGE dbo.Categories AS t
					  USING (SELECT @Category_I AS Category_I, @Name AS Name, @ParentCategory_I AS ParentCategory_I,
					                @Required AS Required, @Archived AS Archived) AS s
					  ON t.Category_I = s.Category_I
					  WHEN MATCHED THEN
					      UPDATE SET Name = s.Name, ParentCategory_I = s.ParentCategory_I,
					                 Required = s.Required, Archived = s.Archived
					  WHEN NOT MATCHED BY TARGET THEN
					      INSERT (Category_I, Name, ParentCategory_I, Required, Archived)
					      VALUES (s.Category_I, s.Name, s.ParentCategory_I, s.Required, s.Archived);",
					cancellationToken,
					[
						new SqlParameter("@Category_I", category.Category_I),
						new SqlParameter("@Name", category.Name),
						new SqlParameter("@ParentCategory_I", (object?)category.ParentCategory_I ?? DBNull.Value),
						new SqlParameter("@Required", category.Required),
						new SqlParameter("@Archived", category.Archived)
					]).ConfigureAwait(false);
			}
		}
		finally
		{
			await ExecuteNonQueryOnConnectionAsync(connection, "SET IDENTITY_INSERT dbo.Categories OFF;", cancellationToken)
				.ConfigureAwait(false);
		}

		await ExecuteNonQueryOnConnectionAsync(connection, "SET IDENTITY_INSERT dbo.PaymentMethods ON;", cancellationToken)
			.ConfigureAwait(false);
		try
		{
			foreach (var paymentMethod in LegacyPaymentMethodSeed.PaymentMethods)
			{
				cancellationToken.ThrowIfCancellationRequested();
				await ExecuteNonQueryOnConnectionAsync(
					connection,
					@"MERGE dbo.PaymentMethods AS t
					  USING (SELECT @Id AS ID, @PaymentMethod AS PaymentMethod) AS s
					  ON t.ID = s.ID
					  WHEN MATCHED THEN
					      UPDATE SET PaymentMethod = s.PaymentMethod
					  WHEN NOT MATCHED BY TARGET THEN
					      INSERT (ID, PaymentMethod) VALUES (s.ID, s.PaymentMethod);",
					cancellationToken,
					[
						new SqlParameter("@Id", paymentMethod.ID),
						new SqlParameter("@PaymentMethod", paymentMethod.PaymentMethodName)
					]).ConfigureAwait(false);
			}
		}
		finally
		{
			await ExecuteNonQueryOnConnectionAsync(connection, "SET IDENTITY_INSERT dbo.PaymentMethods OFF;", cancellationToken)
				.ConfigureAwait(false);
		}

		_logger.LogInformation(
			"Aspire SQL bootstrap: seeded {CategoryCount} categor(ies) and {PaymentMethodCount} payment method(s).",
			LegacyCategorySeed.Categories.Count,
			LegacyPaymentMethodSeed.PaymentMethods.Count);
	}

	private static async Task ExecuteNonQueryOnConnectionAsync(
		SqlConnection connection,
		string commandText,
		CancellationToken cancellationToken,
		IEnumerable<SqlParameter>? parameters = null)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = commandText;
		command.CommandTimeout = 120;
		if (parameters != null)
			command.Parameters.AddRange(parameters.ToArray());
		await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
	}

	private static async Task<object?> ExecuteScalarOnConnectionAsync(
		SqlConnection connection,
		string commandText,
		CancellationToken cancellationToken)
	{
		await using var command = connection.CreateCommand();
		command.CommandText = commandText;
		command.CommandTimeout = 120;
		return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// SQL Server compiles an entire batch before running it; ALTER ADD + MERGE on new columns
	/// must be split on GO (see CreateTables.sql).
	/// </summary>
	private static IReadOnlyList<string> SplitSqlBatches(string sql) =>
		Regex.Split(sql, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
			.Select(batch => batch.Trim())
			.Where(batch => batch.Length > 0)
			.ToList();

	private async Task<Dictionary<int, int>> BuildCategoryMapAsync(CancellationToken cancellationToken)
	{
		var map = new Dictionary<int, int>();
		foreach (var c in LegacyCategorySeed.Categories)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var idObj = await _db.ExecuteScalar(
				"SELECT Category_I FROM dbo.Categories WHERE Category_I = @Id",
				[new SqlParameter("@Id", c.Category_I)]).ConfigureAwait(false);
			if (idObj == null)
				throw new InvalidOperationException($"Category {c.Category_I} '{c.Name}' missing after schema seed.");
			map[c.Category_I] = Convert.ToInt32(idObj);
		}

		return map;
	}

	private async Task<Dictionary<int, int>> BuildPaymentMethodMapAsync(CancellationToken cancellationToken)
	{
		var map = new Dictionary<int, int>();
		foreach (var pm in LegacyPaymentMethodSeed.PaymentMethods)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var idObj = await _db.ExecuteScalar(
				"SELECT ID FROM dbo.PaymentMethods WHERE ID = @Id",
				[new SqlParameter("@Id", pm.ID)]).ConfigureAwait(false);
			if (idObj == null)
				throw new InvalidOperationException($"Payment method {pm.ID} '{pm.PaymentMethodName}' missing after schema seed.");
			map[pm.ID] = Convert.ToInt32(idObj);
		}

		return map;
	}

	private async Task<int> InsertExpenseAsync(
		Expense e,
		Guid seedUserId,
		IReadOnlyDictionary<int, int> categoryMap,
		IReadOnlyDictionary<int, int> paymentMethodMap,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		var createdBy = string.IsNullOrEmpty(e.CreatedBy) ? seedUserId.ToString() : e.CreatedBy;
		var sql = @"INSERT INTO dbo.Expenses (ExpenseDate, Expense, Amount, PaymentMethod, Category, DatePaid, UserId, IsSplit, CreatedDate, ModifiedDate, CreatedBy)
			VALUES (@ExpenseDate, @Expense, @Amount, @PaymentMethod, @Category, @DatePaid, @UserId, @IsSplit, @CreatedDate, @ModifiedDate, @CreatedBy);
			SELECT CAST(SCOPE_IDENTITY() AS INT);";

		object paymentMethodSql = DBNull.Value;
		if (e.PaymentMethod.HasValue)
		{
			if (!paymentMethodMap.TryGetValue(e.PaymentMethod.Value, out var pmResolved))
				throw new InvalidOperationException($"Payment method id {e.PaymentMethod} has no mapped SQL row.");
			paymentMethodSql = pmResolved;
		}

		int? categoryDb = null;
		if (e.Category.HasValue)
		{
			if (!categoryMap.TryGetValue(e.Category.Value, out var catResolved))
				throw new InvalidOperationException($"Category id {e.Category} has no mapped SQL row.");
			categoryDb = catResolved;
		}

		var scalar = await _db.ExecuteScalar(sql,
			[
				new SqlParameter("@ExpenseDate", e.ExpenseDate),
				new SqlParameter("@Expense", e.ExpenseDescription),
				new SqlParameter("@Amount", e.Amount),
				new SqlParameter("@PaymentMethod", paymentMethodSql),
				new SqlParameter("@Category", (object?)categoryDb ?? DBNull.Value),
				new SqlParameter("@DatePaid", (object?)e.DatePaid ?? DBNull.Value),
				new SqlParameter("@UserId", seedUserId),
				new SqlParameter("@IsSplit", e.IsSplit),
				new SqlParameter("@CreatedDate", e.CreatedDateTime),
				new SqlParameter("@ModifiedDate", e.ModifiedDateTime),
				new SqlParameter("@CreatedBy", createdBy)
			]).ConfigureAwait(false);

		return scalar != null ? Convert.ToInt32(scalar) : 0;
	}
}
