using System.ComponentModel;
using System.IO;
using System.Reflection;
using MoneyManager.Core.Models;
using MoneyManager.Data.Repositories;
using MoneyManager.Data.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MoneyManager.Data.Bootstrap;

/// <summary>
/// Under .NET Aspire with SQL Server: applies schema (CreateTables.sql), ensures mock catalog rows exist,
/// and inserts <see cref="MockData"/> expenses once per seed user when the database is empty for that user.
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

		const int maxAttempts = 30;
		for (var attempt = 1; attempt <= maxAttempts; attempt++)
		{
			try
			{
				await RunBootstrapCoreAsync(seedUserId, cancellationToken).ConfigureAwait(false);
				return;
			}
			catch (Exception ex) when (attempt < maxAttempts && IsSqlConnectivityTransient(ex))
			{
				_logger.LogWarning(
					ex,
					"Aspire SQL bootstrap attempt {Attempt}/{Max} failed (SQL may still be starting); retrying in 2s.",
					attempt,
					maxAttempts);
				await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
			}
		}
	}

	private async Task RunBootstrapCoreAsync(Guid seedUserId, CancellationToken cancellationToken)
	{
		var schemaSql = await LoadSchemaSqlAsync(cancellationToken).ConfigureAwait(false);
		await ApplySchemaAsync(schemaSql, cancellationToken).ConfigureAwait(false);

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
		await using var connection = _connectionFactory.CreateConnection();
		await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
		await using var command = connection.CreateCommand();
		command.CommandText = schemaSql;
		command.CommandTimeout = 120;
		await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
		_logger.LogInformation("Aspire SQL bootstrap: applied schema script.");
	}

	private async Task<Dictionary<int, int>> BuildCategoryMapAsync(CancellationToken cancellationToken)
	{
		var map = new Dictionary<int, int>();
		foreach (var c in MockData.Categories)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await EnsureCategoryAsync(c.Name, cancellationToken).ConfigureAwait(false);
			var idObj = await _db.ExecuteScalar(
				"SELECT Category_I FROM dbo.Categories WHERE Name = @Name",
				[new SqlParameter("@Name", c.Name)]).ConfigureAwait(false);
			if (idObj == null)
				throw new InvalidOperationException($"Category '{c.Name}' missing after ensure.");
			map[c.Category_I] = Convert.ToInt32(idObj);
		}

		return map;
	}

	private Task EnsureCategoryAsync(string name, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return _db.ExecuteNonQuery(
			@"IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = @Name)
				INSERT INTO dbo.Categories (Name) VALUES (@Name);",
			[new SqlParameter("@Name", name)]);
	}

	private async Task<Dictionary<int, int>> BuildPaymentMethodMapAsync(CancellationToken cancellationToken)
	{
		var map = new Dictionary<int, int>();
		foreach (var pm in MockData.PaymentMethods)
		{
			cancellationToken.ThrowIfCancellationRequested();
			await EnsurePaymentMethodAsync(pm.PaymentMethodName, cancellationToken).ConfigureAwait(false);
			var idObj = await _db.ExecuteScalar(
				"SELECT ID FROM dbo.PaymentMethods WHERE PaymentMethod = @Name",
				[new SqlParameter("@Name", pm.PaymentMethodName)]).ConfigureAwait(false);
			if (idObj == null)
				throw new InvalidOperationException($"Payment method '{pm.PaymentMethodName}' missing after ensure.");
			map[pm.ID] = Convert.ToInt32(idObj);
		}

		return map;
	}

	private Task EnsurePaymentMethodAsync(string name, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return _db.ExecuteNonQuery(
			@"IF NOT EXISTS (SELECT 1 FROM dbo.PaymentMethods WHERE PaymentMethod = @Name)
				INSERT INTO dbo.PaymentMethods (PaymentMethod) VALUES (@Name);",
			[new SqlParameter("@Name", name)]);
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
