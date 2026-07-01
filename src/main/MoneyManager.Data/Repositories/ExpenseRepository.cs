using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core;
using MoneyManager.Core.Constants;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Data.Expenses;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Repositories
{
	public class ExpenseRepository : IExpenseRepository
	{
		private readonly DbExecutor _db;
		private readonly IExpenseMapper _readerMapper;
		private readonly ExpenseDomainMapper _domainMapper;
		private readonly INowProvider _nowProvider;
		private readonly IExpensePatchDbUpdateBuilder _patchDbUpdateBuilder;

		public ExpenseRepository(
			DbExecutor db,
			IExpenseMapper readerMapper,
			ExpenseDomainMapper domainMapper,
			INowProvider nowProvider,
			IExpensePatchDbUpdateBuilder patchDbUpdateBuilder)
		{
			_db = db;
			_readerMapper = readerMapper;
			_domainMapper = domainMapper;
			_nowProvider = nowProvider;
			_patchDbUpdateBuilder = patchDbUpdateBuilder;
		}

		public async Task<Expense?> Get(int id, Guid userId)
		{
			var db = await GetDb(id, userId);
			return db != null ? _domainMapper.ToExpense(db) : null;
		}

		public async Task<IReadOnlyList<Expense>> ListForUser(Guid userId, string? month = null)
		{
			var listDb = await ListForUserDb(userId, month);
			return listDb.Select(_domainMapper.ToExpense).ToList();
		}

		public async Task<IReadOnlyList<Expense>> ListForUserWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null, string? currency = null)
		{
			var result = new List<DbExpense>();
			var sql = "SELECT * FROM Expenses WHERE UserId = @UserId";
			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@UserId", userId)
			};

			if (paymentMethod.HasValue)
			{
				sql += " AND PaymentMethod = @PaymentMethod";
				parameters.Add(new SqlParameter("@PaymentMethod", paymentMethod.Value));
			}

			if (datePaidNull == true)
			{
				sql += " AND DatePaid IS NULL";
			}

			if (!string.IsNullOrWhiteSpace(currency))
			{
				sql += " AND ISNULL(Currency, 'USD') = @Currency";
				parameters.Add(new SqlParameter("@Currency", currency));
			}

			sql += " ORDER BY ExpenseDate ASC";

			await _db.ExecuteReader(sql, parameters, async sqlReader =>
			{
				while (await sqlReader.ReadAsync())
				{
					result.Add(await _readerMapper.FromDbReader(sqlReader));
				}
			});
			return result.Select(_domainMapper.ToExpense).ToList();
		}

		public async Task<Expense?> Create(Guid userId, CreateExpenseModel model)
		{
			var db = _domainMapper.ToDbExpense(model, userId);
			var id = await SaveDb(userId, db);
			if (id <= 0) return null;
			return await Get(id, userId);
		}

		public async Task<UpdateExpenseResult> Update(int id, Guid userId, Expense expense)
		{
			var existing = await GetDb(id, userId);
			if (existing == null) return UpdateExpenseResult.NotFound();
			if (!ExpenseConcurrency.ModifiedUtcMillisEqual(existing.ModifiedDate, expense.ModifiedDateTime))
			{
				var current = _domainMapper.ToExpense(existing);
				return current != null ? UpdateExpenseResult.Conflict(current) : UpdateExpenseResult.NotFound();
			}
			_domainMapper.Update(existing, expense);
			var rowsAffected = await SaveDb(userId, existing, expense.ModifiedDateTime);
			if (rowsAffected == 0)
			{
				var current = _domainMapper.ToExpense(existing);
				return current != null ? UpdateExpenseResult.Conflict(current) : UpdateExpenseResult.NotFound();
			}
			var updated = await Get(id, userId);
			return updated != null ? UpdateExpenseResult.Success(updated) : UpdateExpenseResult.NotFound();
		}

		public async Task<UpdateExpenseResult> Patch(int id, Guid userId, Dictionary<string, object?> updates, DateTime? expectedModifiedDateTime)
		{
			var updatesCopy = new Dictionary<string, object?>(updates);
			updatesCopy.Remove(ExpenseFieldNames.ModifiedDateTime);
			updatesCopy.Remove(ExpenseFieldNames.CreatedDateTime);
			var success = await UpdateDb(id, userId, updatesCopy, expectedModifiedDateTime);
			if (!success)
			{
				var current = await Get(id, userId);
				return current != null ? UpdateExpenseResult.Conflict(current) : UpdateExpenseResult.NotFound();
			}
			var updated = await Get(id, userId);
			return updated != null ? UpdateExpenseResult.Success(updated) : UpdateExpenseResult.NotFound();
		}

		public async Task<bool> Delete(int id, Guid userId)
		{
			var rowsAffected = await _db.ExecuteNonQuery(
				"DELETE FROM Expenses WHERE Expense_I = @Id AND UserId = @UserId",
				[
					new SqlParameter("@Id", id),
					new SqlParameter("@UserId", userId)
				]);
			return rowsAffected > 0;
		}

		public async Task<bool> BulkUpdate(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates)
		{
			if (!ids.Any() || !updates.Any())
				return false;

			var idList = string.Join(",", ids);
			var setClauses = new List<string>();
			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@UserId", userId)
			};

			if (!_patchDbUpdateBuilder.AppendBulkSetClauses(updates, setClauses, parameters))
				return false;

			setClauses.Add("ModifiedDate = @ModifiedDate");
			parameters.Add(new SqlParameter("@ModifiedDate", _nowProvider.UtcNow));

			var sql = $"UPDATE Expenses SET {string.Join(", ", setClauses)} WHERE Expense_I IN ({idList}) AND UserId = @UserId";

			var rowsAffected = await _db.ExecuteNonQuery(sql, parameters);
			return rowsAffected > 0;
		}

		public async Task<bool> BulkDelete(IEnumerable<int> ids, Guid userId)
		{
			if (!ids.Any())
				return false;

			var idList = string.Join(",", ids);
			var sql = $"DELETE FROM Expenses WHERE Expense_I IN ({idList}) AND UserId = @UserId";

			var rowsAffected = await _db.ExecuteNonQuery(sql, [
				new SqlParameter("@UserId", userId)
			]);
			return rowsAffected > 0;
		}

		public async Task<IReadOnlyList<Expense>> ListForUserInDateRange(Guid userId, DateTime fromDate, DateTime toDate, int? paymentMethodId = null)
		{
			var result = new List<DbExpense>();
			var sql = paymentMethodId.HasValue
				? "SELECT * FROM Expenses WHERE UserId = @UserId AND ExpenseDate >= @FromDate AND ExpenseDate <= @ToDate AND PaymentMethod = @PaymentMethod ORDER BY ExpenseDate DESC"
				: "SELECT * FROM Expenses WHERE UserId = @UserId AND ExpenseDate >= @FromDate AND ExpenseDate <= @ToDate ORDER BY ExpenseDate DESC";
			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@UserId", userId),
				new SqlParameter("@FromDate", fromDate.Date),
				new SqlParameter("@ToDate", toDate.Date)
			};
			if (paymentMethodId.HasValue)
				parameters.Add(new SqlParameter("@PaymentMethod", paymentMethodId.Value));
			await _db.ExecuteReader(sql, parameters, async sqlReader =>
			{
				while (await sqlReader.ReadAsync())
					result.Add(await _readerMapper.FromDbReader(sqlReader));
			});
			return result.Select(_domainMapper.ToExpense).ToList();
		}

		public async Task<IReadOnlyList<Expense>> SearchForUser(
			Guid userId,
			DateTime fromDate,
			DateTime toDate,
			string? search,
			IReadOnlyList<int>? categoryIds)
		{
			var result = new List<DbExpense>();
			var sql = "SELECT * FROM Expenses WHERE UserId = @UserId AND ExpenseDate >= @FromDate AND ExpenseDate <= @ToDate";
			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@UserId", userId),
				new SqlParameter("@FromDate", fromDate.Date),
				new SqlParameter("@ToDate", toDate.Date)
			};

			if (!string.IsNullOrWhiteSpace(search))
			{
				sql += " AND Expense LIKE @Search ESCAPE '\\'";
				parameters.Add(new SqlParameter("@Search", $"%{EscapeLikePattern(search)}%"));
			}

			if (categoryIds is { Count: > 0 })
			{
				var placeholders = string.Join(",", categoryIds.Select((_, i) => $"@Cat{i}"));
				sql += $" AND Category IN ({placeholders})";
				for (var i = 0; i < categoryIds.Count; i++)
					parameters.Add(new SqlParameter($"@Cat{i}", categoryIds[i]));
			}

			sql += " ORDER BY ExpenseDate DESC";

			await _db.ExecuteReader(sql, parameters, async sqlReader =>
			{
				while (await sqlReader.ReadAsync())
					result.Add(await _readerMapper.FromDbReader(sqlReader));
			});
			return result.Select(_domainMapper.ToExpense).ToList();
		}

		private static string EscapeLikePattern(string value)
		{
			return value
				.Replace("\\", "\\\\")
				.Replace("%", "\\%")
				.Replace("_", "\\_")
				.Replace("[", "\\[");
		}

		public async Task<IReadOnlyList<LastImportDatesForPaymentMethod>> GetLastImportDates(Guid userId, IReadOnlyList<int> paymentMethodIds)
		{
			if (paymentMethodIds.Count == 0)
				return Array.Empty<LastImportDatesForPaymentMethod>();
			var dict = new Dictionary<int, (DateTime? LatestExpenseDate, DateTime? LatestDatePaid)>();
			foreach (var id in paymentMethodIds)
				dict[id] = (null, null);
			var idParams = string.Join(",", paymentMethodIds.Select((_, i) => "@pm" + i));
			var sql = $"SELECT PaymentMethod, MAX(ExpenseDate) AS LatestExpenseDate, MAX(DatePaid) AS LatestDatePaid FROM Expenses WHERE UserId = @UserId AND CreatedBy = @CreatedBy AND PaymentMethod IN ({idParams}) GROUP BY PaymentMethod";
			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@UserId", userId),
				new SqlParameter("@CreatedBy", ExpenseConstants.ImportCreatedBy)
			};
			for (var i = 0; i < paymentMethodIds.Count; i++)
				parameters.Add(new SqlParameter("@pm" + i, paymentMethodIds[i]));
			await _db.ExecuteReader(sql, parameters, async sqlReader =>
			{
				while (await sqlReader.ReadAsync())
				{
					var pmId = sqlReader.GetInt32(sqlReader.GetOrdinal("PaymentMethod"));
					var latestExpenseDate = sqlReader.IsDBNull(sqlReader.GetOrdinal("LatestExpenseDate")) ? (DateTime?)null : sqlReader.GetDateTime(sqlReader.GetOrdinal("LatestExpenseDate"));
					var latestDatePaid = sqlReader.IsDBNull(sqlReader.GetOrdinal("LatestDatePaid")) ? (DateTime?)null : sqlReader.GetDateTime(sqlReader.GetOrdinal("LatestDatePaid"));
					dict[pmId] = (latestExpenseDate, latestDatePaid);
				}
			});
			return paymentMethodIds.Select(id => new LastImportDatesForPaymentMethod
			{
				PaymentMethodId = id,
				LatestExpenseDate = dict[id].LatestExpenseDate,
				LatestDatePaid = dict[id].LatestDatePaid
			}).ToList();
		}

		private async Task<DbExpense?> GetDb(int id, Guid userId)
		{
			var result = default(DbExpense?);
			await _db.ExecuteReader(
				"SELECT * FROM Expenses WHERE Expense_I = @Id AND UserId = @UserId",
				[
					new SqlParameter("@Id", id),
					new SqlParameter("@UserId", userId)
				],
				async sqlReader =>
				{
					if (await sqlReader.ReadAsync())
						result = await _readerMapper.FromDbReader(sqlReader);
				});
			return result;
		}

		private async Task<List<DbExpense>> ListForUserDb(Guid userId, string? month = null)
		{
			var result = new List<DbExpense>();
			var sql = "SELECT * FROM Expenses WHERE UserId = @UserId";
			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@UserId", userId)
			};

			if (!string.IsNullOrEmpty(month))
			{
				sql += " AND FORMAT(ExpenseDate, 'yyyy-MM') = @Month";
				parameters.Add(new SqlParameter("@Month", month));
			}

			sql += " ORDER BY ExpenseDate ASC";

			await _db.ExecuteReader(sql, parameters, async sqlReader =>
			{
				while (await sqlReader.ReadAsync())
					result.Add(await _readerMapper.FromDbReader(sqlReader));
			});
			return result;
		}

		private async Task<int> SaveDb(Guid userId, DbExpense expense, DateTime? expectedModifiedDateTime = null)
		{
			if (expense.Expense_I == 0)
			{
				var sql = @"INSERT INTO Expenses (ExpenseDate, Expense, Amount, Currency, PaymentMethod, Category, DatePaid, UserId, IsSplit, ExcludeFromCredit, CreatedDate, ModifiedDate, CreatedBy)
							VALUES (@ExpenseDate, @Expense, @Amount, @Currency, @PaymentMethod, @Category, @DatePaid, @UserId, @IsSplit, @ExcludeFromCredit, @CreatedDate, @ModifiedDate, @CreatedBy);
							SELECT CAST(SCOPE_IDENTITY() as int);";

				var now = _nowProvider.UtcNow;
				var scalar = await _db.ExecuteScalar(sql, [
					new SqlParameter("@ExpenseDate", expense.ExpenseDate),
					new SqlParameter("@Expense", expense.Expense),
					new SqlParameter("@Amount", expense.Amount),
					new SqlParameter("@Currency", string.IsNullOrWhiteSpace(expense.Currency) ? CurrencyConstants.Default : expense.Currency),
					new SqlParameter("@PaymentMethod", (object?)expense.PaymentMethod ?? DBNull.Value),
					new SqlParameter("@Category", (object?)expense.Category ?? DBNull.Value),
					new SqlParameter("@DatePaid", (object?)expense.DatePaid ?? DBNull.Value),
					new SqlParameter("@UserId", userId),
					new SqlParameter("@IsSplit", expense.IsSplit),
					new SqlParameter("@ExcludeFromCredit", expense.ExcludeFromCredit),
					new SqlParameter("@CreatedDate", now),
					new SqlParameter("@ModifiedDate", now),
					new SqlParameter("@CreatedBy", expense.CreatedBy)
				]);

				return scalar != null ? Convert.ToInt32(scalar) : 0;
			}

			var updateSql = @"UPDATE Expenses 
				SET ExpenseDate = @ExpenseDate, Expense = @Expense, Amount = @Amount, Currency = @Currency,
					PaymentMethod = @PaymentMethod, Category = @Category, DatePaid = @DatePaid, 
					IsSplit = @IsSplit, ExcludeFromCredit = @ExcludeFromCredit, ModifiedDate = @ModifiedDate
				WHERE Expense_I = @Id AND UserId = @UserId
					AND CAST(ModifiedDate AS datetime2(3)) = CAST(@ExpectedModifiedDate AS datetime2(3))";

			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@Id", expense.Expense_I),
				new SqlParameter("@ExpenseDate", expense.ExpenseDate),
				new SqlParameter("@Expense", expense.Expense),
				new SqlParameter("@Amount", expense.Amount),
				new SqlParameter("@PaymentMethod", (object?)expense.PaymentMethod ?? DBNull.Value),
				new SqlParameter("@Category", (object?)expense.Category ?? DBNull.Value),
				new SqlParameter("@DatePaid", (object?)expense.DatePaid ?? DBNull.Value),
				new SqlParameter("@IsSplit", expense.IsSplit),
				new SqlParameter("@ExcludeFromCredit", expense.ExcludeFromCredit),
				new SqlParameter("@ModifiedDate", _nowProvider.UtcNow),
				new SqlParameter("@UserId", userId),
				new SqlParameter("@ExpectedModifiedDate", (object?)expectedModifiedDateTime ?? DBNull.Value)
			};

			var rowsAffected = await _db.ExecuteNonQuery(updateSql, parameters);
			return rowsAffected;
		}

		private async Task<bool> UpdateDb(int id, Guid userId, Dictionary<string, object?> updates, DateTime? expectedModifiedDateTime = null)
		{
			if (!updates.Any())
				return false;

			var setClauses = new List<string>();
			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@Id", id),
				new SqlParameter("@UserId", userId)
			};

			if (!_patchDbUpdateBuilder.AppendPatchSetClauses(updates, setClauses, parameters))
				return false;

			setClauses.Add("ModifiedDate = @ModifiedDate");
			parameters.Add(new SqlParameter("@ModifiedDate", _nowProvider.UtcNow));

			var whereConcurrency = "";
			if (expectedModifiedDateTime.HasValue)
			{
				whereConcurrency = " AND CAST(ModifiedDate AS datetime2(3)) = CAST(@ExpectedModifiedDate AS datetime2(3))";
				parameters.Add(new SqlParameter("@ExpectedModifiedDate", expectedModifiedDateTime.Value));
			}
			var sql = $"UPDATE Expenses SET {string.Join(", ", setClauses)} WHERE Expense_I = @Id AND UserId = @UserId{whereConcurrency}";

			var rowsAffected = await _db.ExecuteNonQuery(sql, parameters);
			return rowsAffected > 0;
		}
	}
}
