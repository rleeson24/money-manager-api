using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
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

		public ExpenseRepository(DbExecutor db, IExpenseMapper readerMapper, ExpenseDomainMapper domainMapper)
		{
			_db = db;
			_readerMapper = readerMapper;
			_domainMapper = domainMapper;
		}

		public async Task<Expense?> Get(int id, Guid userId)
		{
			var db = await GetDb(id, userId);
			return db != null ? _domainMapper.ToExpense(db) : null;
		}

		public async Task<IEnumerable<Expense>> ListForUser(Guid userId, string? month = null)
		{
			var list = await ListForUserDb(userId, month);
			return list.Select(_domainMapper.ToExpense);
		}

		public async Task<Expense?> Create(Guid userId, CreateExpenseModel model)
		{
			var db = _domainMapper.ToDbExpense(model, userId);
			var id = await SaveDb(userId, db);
			if (id <= 0) return null;
			return await Get(id, userId);
		}

		public async Task<Expense?> Update(int id, Guid userId, CreateExpenseModel model)
		{
			var existing = await GetDb(id, userId);
			if (existing == null) return null;
			_domainMapper.Update(existing, model);
			await SaveDb(userId, existing);
			return await Get(id, userId);
		}

		public async Task<Expense?> Patch(int id, Guid userId, Dictionary<string, object?> updates)
		{
			var success = await UpdateDb(id, userId, updates);
			if (!success) return null;
			return await Get(id, userId);
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

			if (updates.ContainsKey("ExpenseDate") && updates["ExpenseDate"] != null)
			{
				setClauses.Add("ExpenseDate = @ExpenseDate");
				parameters.Add(new SqlParameter("@ExpenseDate", updates["ExpenseDate"]));
			}
			if (updates.ContainsKey("Category"))
			{
				if (updates["Category"] == null)
					setClauses.Add("Category = NULL");
				else
				{
					setClauses.Add("Category = @Category");
					parameters.Add(new SqlParameter("@Category", updates["Category"]));
				}
			}
			if (updates.ContainsKey("DatePaid"))
			{
				if (updates["DatePaid"] == null)
					setClauses.Add("DatePaid = NULL");
				else
				{
					setClauses.Add("DatePaid = @DatePaid");
					parameters.Add(new SqlParameter("@DatePaid", updates["DatePaid"]));
				}
			}

			if (!setClauses.Any())
				return false;

			setClauses.Add("ModifiedDate = @ModifiedDate");
			parameters.Add(new SqlParameter("@ModifiedDate", DateTime.UtcNow));

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

			sql += " ORDER BY ExpenseDate DESC";

			await _db.ExecuteReader(sql, parameters, async sqlReader =>
			{
				while (await sqlReader.ReadAsync())
					result.Add(await _readerMapper.FromDbReader(sqlReader));
			});
			return result;
		}

		private async Task<int> SaveDb(Guid userId, DbExpense expense)
		{
			if (expense.Expense_I == 0)
			{
				var sql = @"INSERT INTO Expenses (ExpenseDate, Expense, Amount, PaymentMethod, Category, DatePaid, UserId, CreatedDate)
							VALUES (@ExpenseDate, @Expense, @Amount, @PaymentMethod, @Category, @DatePaid, @UserId, @CreatedDate);
							SELECT CAST(SCOPE_IDENTITY() as int);";

				var scalar = await _db.ExecuteScalar(sql, [
					new SqlParameter("@ExpenseDate", expense.ExpenseDate),
					new SqlParameter("@Expense", expense.Expense),
					new SqlParameter("@Amount", expense.Amount),
					new SqlParameter("@PaymentMethod", (object?)expense.PaymentMethod ?? DBNull.Value),
					new SqlParameter("@Category", (object?)expense.Category ?? DBNull.Value),
					new SqlParameter("@DatePaid", (object?)expense.DatePaid ?? DBNull.Value),
					new SqlParameter("@UserId", userId),
					new SqlParameter("@CreatedDate", DateTime.UtcNow)
				]);

				return scalar != null ? Convert.ToInt32(scalar) : 0;
			}

			var updateSql = @"UPDATE Expenses 
				SET ExpenseDate = @ExpenseDate, Expense = @Expense, Amount = @Amount, 
					PaymentMethod = @PaymentMethod, Category = @Category, DatePaid = @DatePaid, 
					ModifiedDate = @ModifiedDate
				WHERE Expense_I = @Id AND UserId = @UserId";

			await _db.ExecuteNonQuery(updateSql, [
				new SqlParameter("@Id", expense.Expense_I),
				new SqlParameter("@ExpenseDate", expense.ExpenseDate),
				new SqlParameter("@Expense", expense.Expense),
				new SqlParameter("@Amount", expense.Amount),
				new SqlParameter("@PaymentMethod", (object?)expense.PaymentMethod ?? DBNull.Value),
				new SqlParameter("@Category", (object?)expense.Category ?? DBNull.Value),
				new SqlParameter("@DatePaid", (object?)expense.DatePaid ?? DBNull.Value),
				new SqlParameter("@ModifiedDate", DateTime.UtcNow),
				new SqlParameter("@UserId", userId)
			]);

			return expense.Expense_I;
		}

		private async Task<bool> UpdateDb(int id, Guid userId, Dictionary<string, object?> updates)
		{
			if (!updates.Any())
				return false;

			var setClauses = new List<string>();
			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@Id", id),
				new SqlParameter("@UserId", userId)
			};

			if (updates.ContainsKey("ExpenseDate") && updates["ExpenseDate"] != null)
			{
				setClauses.Add("ExpenseDate = @ExpenseDate");
				parameters.Add(new SqlParameter("@ExpenseDate", updates["ExpenseDate"]));
			}
			if (updates.ContainsKey("Expense") && updates["Expense"] != null)
			{
				setClauses.Add("Expense = @Expense");
				parameters.Add(new SqlParameter("@Expense", updates["Expense"]));
			}
			if (updates.ContainsKey("Amount") && updates["Amount"] != null)
			{
				setClauses.Add("Amount = @Amount");
				parameters.Add(new SqlParameter("@Amount", updates["Amount"]));
			}
			if (updates.ContainsKey("PaymentMethod"))
			{
				if (updates["PaymentMethod"] == null)
					setClauses.Add("PaymentMethod = NULL");
				else
				{
					setClauses.Add("PaymentMethod = @PaymentMethod");
					parameters.Add(new SqlParameter("@PaymentMethod", updates["PaymentMethod"]));
				}
			}
			if (updates.ContainsKey("Category"))
			{
				if (updates["Category"] == null)
					setClauses.Add("Category = NULL");
				else
				{
					setClauses.Add("Category = @Category");
					parameters.Add(new SqlParameter("@Category", updates["Category"]));
				}
			}
			if (updates.ContainsKey("DatePaid"))
			{
				if (updates["DatePaid"] == null)
					setClauses.Add("DatePaid = NULL");
				else
				{
					setClauses.Add("DatePaid = @DatePaid");
					parameters.Add(new SqlParameter("@DatePaid", updates["DatePaid"]));
				}
			}

			if (!setClauses.Any())
				return false;

			setClauses.Add("ModifiedDate = @ModifiedDate");
			parameters.Add(new SqlParameter("@ModifiedDate", DateTime.UtcNow));

			var sql = $"UPDATE Expenses SET {string.Join(", ", setClauses)} WHERE Expense_I = @Id AND UserId = @UserId";

			var rowsAffected = await _db.ExecuteNonQuery(sql, parameters);
			return rowsAffected > 0;
		}
	}
}
