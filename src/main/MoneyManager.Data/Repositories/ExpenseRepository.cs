using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Repositories
{
	public class ExpenseRepository : IExpenseRepository
	{
		private readonly DbExecutor _db;
		private readonly IExpenseMapper _mapper;

		public ExpenseRepository(DbExecutor db, IExpenseMapper mapper)
		{
			_db = db;
			_mapper = mapper;
		}

		public async Task<DbExpense?> Get(int id, Guid userId)
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
					{
						result = await _mapper.FromDbReader(sqlReader);
					}
				});
			return result;
		}

		public async Task<IEnumerable<DbExpense>> ListForUser(Guid userId, string? month = null)
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
				{
					result.Add(await _mapper.FromDbReader(sqlReader));
				}
			});
			return result;
		}

		public async Task<int> Save(Guid userId, DbExpense expense)
		{
			if (expense.Expense_I == 0)
			{
				// Insert
				var sql = @"INSERT INTO Expenses (ExpenseDate, Expense, Amount, PaymentMethod, Category, DatePaid, UserId, CreatedDate)
							VALUES (@ExpenseDate, @Expense, @Amount, @PaymentMethod, @Category, @DatePaid, @UserId, @CreatedDate);
							SELECT CAST(SCOPE_IDENTITY() as int);";

				var result = await _db.ExecuteScalar(sql, [
					new SqlParameter("@ExpenseDate", expense.ExpenseDate),
					new SqlParameter("@Expense", expense.Expense),
					new SqlParameter("@Amount", expense.Amount),
					new SqlParameter("@PaymentMethod", expense.PaymentMethod),
					new SqlParameter("@Category", expense.Category),
					new SqlParameter("@DatePaid", (object?)expense.DatePaid ?? DBNull.Value),
					new SqlParameter("@UserId", userId),
					new SqlParameter("@CreatedDate", DateTime.UtcNow)
				]);

				return result != null ? Convert.ToInt32(result) : 0;
			}
			else
			{
				// Update
				var sql = @"UPDATE Expenses 
							SET ExpenseDate = @ExpenseDate, Expense = @Expense, Amount = @Amount, 
								PaymentMethod = @PaymentMethod, Category = @Category, DatePaid = @DatePaid, 
								ModifiedDate = @ModifiedDate
							WHERE Expense_I = @Id AND UserId = @UserId";

				await _db.ExecuteNonQuery(sql, [
					new SqlParameter("@Id", expense.Expense_I),
					new SqlParameter("@ExpenseDate", expense.ExpenseDate),
					new SqlParameter("@Expense", expense.Expense),
					new SqlParameter("@Amount", expense.Amount),
					new SqlParameter("@PaymentMethod", expense.PaymentMethod),
					new SqlParameter("@Category", expense.Category),
					new SqlParameter("@DatePaid", (object?)expense.DatePaid ?? DBNull.Value),
					new SqlParameter("@ModifiedDate", DateTime.UtcNow),
					new SqlParameter("@UserId", userId)
				]);

				return expense.Expense_I;
			}
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
			if (updates.ContainsKey("Category") && updates["Category"] != null)
			{
				setClauses.Add("Category = @Category");
				parameters.Add(new SqlParameter("@Category", updates["Category"]));
			}
			if (updates.ContainsKey("DatePaid"))
			{
				if (updates["DatePaid"] == null)
				{
					setClauses.Add("DatePaid = NULL");
				}
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

		public async Task<bool> Update(int id, Guid userId, Dictionary<string, object?> updates)
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
			if (updates.ContainsKey("PaymentMethod") && updates["PaymentMethod"] != null)
			{
				setClauses.Add("PaymentMethod = @PaymentMethod");
				parameters.Add(new SqlParameter("@PaymentMethod", updates["PaymentMethod"]));
			}
			if (updates.ContainsKey("Category") && updates["Category"] != null)
			{
				setClauses.Add("Category = @Category");
				parameters.Add(new SqlParameter("@Category", updates["Category"]));
			}
			if (updates.ContainsKey("DatePaid"))
			{
				if (updates["DatePaid"] == null)
				{
					setClauses.Add("DatePaid = NULL");
				}
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
