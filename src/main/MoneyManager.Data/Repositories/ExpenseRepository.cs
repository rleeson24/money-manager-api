using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Core.UseCases.Expenses;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using DataOptions = MoneyManager.Data.DataOptions;

namespace MoneyManager.Data.Repositories
{
	public class ExpenseRepository : IExpenseRepository
	{
		private readonly DbExecutor _db;
		private readonly IExpenseMapper _readerMapper;
		private readonly ExpenseDomainMapper _domainMapper;
		private readonly INowProvider _nowProvider;
		private readonly DataOptions _dataOptions;

		public ExpenseRepository(DbExecutor db, IExpenseMapper readerMapper, ExpenseDomainMapper domainMapper, INowProvider nowProvider, IOptions<DataOptions> dataOptions)
		{
			_db = db;
			_readerMapper = readerMapper;
			_domainMapper = domainMapper;
			_nowProvider = nowProvider;
			_dataOptions = dataOptions.Value;
		}

		public async Task<Expense?> Get(int id, Guid userId)
		{
			if (_dataOptions.UseMockData)
			{
				var found = MockData.Expenses.FirstOrDefault(e => e.Expense_I == id);
				return await Task.FromResult(found);
			}
			var db = await GetDb(id, userId);
			return db != null ? _domainMapper.ToExpense(db) : null;
		}

		public async Task<IReadOnlyList<Expense>> ListForUser(Guid userId, string? month = null)
		{
			if (_dataOptions.UseMockData)
			{
				var list = MockData.Expenses.AsEnumerable();
				if (!string.IsNullOrEmpty(month))
					list = list.Where(e => e.ExpenseDate.ToString("yyyy-MM") == month);
				return await Task.FromResult(list.OrderByDescending(e => e.ExpenseDate).ToList());
			}
			var listDb = await ListForUserDb(userId, month);
			return listDb.Select(_domainMapper.ToExpense).ToList();
		}

		

		public async Task<IReadOnlyList<Expense>> ListForUserWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null)
		{
			if (_dataOptions.UseMockData)
			{
				var list = MockData.Expenses.AsEnumerable();
				if (paymentMethod.HasValue)
					list = list.Where(e => e.PaymentMethod == paymentMethod.Value);
				if (datePaidNull == true)
					list = list.Where(e => e.DatePaid == null);
				return await Task.FromResult(list.OrderByDescending(e => e.ExpenseDate).ToList());
			}
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

			sql += " ORDER BY ExpenseDate DESC";

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
			if (_dataOptions.UseMockData)
			{
				var nextId = MockData.Expenses.Count > 0 ? MockData.Expenses.Max(e => e.Expense_I) + 1 : 1;
				var now = _nowProvider.UtcNow;
				return await Task.FromResult(new Expense
				{
					Expense_I = nextId,
					ExpenseDate = model.ExpenseDate,
					ExpenseDescription = model.Expense,
					Amount = model.Amount,
					PaymentMethod = model.PaymentMethod,
					Category = model.Category,
					DatePaid = model.DatePaid,
					CreatedDateTime = now,
					ModifiedDateTime = now
				});
			}
			var db = _domainMapper.ToDbExpense(model, userId);
			var id = await SaveDb(userId, db);
			if (id <= 0) return null;
			return await Get(id, userId);
		}

		public async Task<UpdateExpenseResult> Update(int id, Guid userId, Expense expense)
		{
			if (_dataOptions.UseMockData)
				return await Task.FromResult(UpdateExpenseResult.Success(expense));

			var existing = await GetDb(id, userId);
			if (existing == null) return UpdateExpenseResult.NotFound();
			// Optimistic concurrency: only update if ModifiedDateTime matches
			if (existing.ModifiedDate != expense.ModifiedDateTime)
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
			if (_dataOptions.UseMockData)
			{
				var current = MockData.Expenses.FirstOrDefault(e => e.Expense_I == id);
				if (current == null) return UpdateExpenseResult.NotFound();
				var patched = new Expense
				{
					Expense_I = current.Expense_I,
					ExpenseDate = updates.TryGetValue("ExpenseDate", out var d) && d is DateTime dt ? dt : current.ExpenseDate,
					ExpenseDescription = updates.TryGetValue("Expense", out var desc) && desc is string s ? s : current.ExpenseDescription,
					Amount = updates.TryGetValue("Amount", out var a) && a is decimal amt ? amt : current.Amount,
					PaymentMethod = updates.ContainsKey("PaymentMethod") ? (int?)updates["PaymentMethod"] : current.PaymentMethod,
					Category = updates.ContainsKey("Category") ? (int?)updates["Category"] : current.Category,
					DatePaid = updates.ContainsKey("DatePaid") ? (DateTime?)updates["DatePaid"] : current.DatePaid,
					CreatedDateTime = current.CreatedDateTime,
					ModifiedDateTime = _nowProvider.UtcNow
				};
				return await Task.FromResult(UpdateExpenseResult.Success(patched));
			}
			// Don't persist timestamp fields as column updates
			var updatesCopy = new Dictionary<string, object?>(updates);
			updatesCopy.Remove("ModifiedDateTime");
			updatesCopy.Remove("CreatedDateTime");
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
			if (_dataOptions.UseMockData)
				return await Task.FromResult(MockData.Expenses.Any(e => e.Expense_I == id));

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
			if (_dataOptions.UseMockData)
				return await Task.FromResult(ids.Any());

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
			parameters.Add(new SqlParameter("@ModifiedDate", _nowProvider.UtcNow));

			var sql = $"UPDATE Expenses SET {string.Join(", ", setClauses)} WHERE Expense_I IN ({idList}) AND UserId = @UserId";

			var rowsAffected = await _db.ExecuteNonQuery(sql, parameters);
			return rowsAffected > 0;
		}

		public async Task<bool> BulkDelete(IEnumerable<int> ids, Guid userId)
		{
			if (_dataOptions.UseMockData)
				return await Task.FromResult(ids.Any());

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

		private async Task<int> SaveDb(Guid userId, DbExpense expense, DateTime? expectedModifiedDateTime = null)
		{
			if (expense.Expense_I == 0)
			{
				var sql = @"INSERT INTO Expenses (ExpenseDate, Expense, Amount, PaymentMethod, Category, DatePaid, UserId, CreatedDate, ModifiedDate)
							VALUES (@ExpenseDate, @Expense, @Amount, @PaymentMethod, @Category, @DatePaid, @UserId, @CreatedDate, @ModifiedDate);
							SELECT CAST(SCOPE_IDENTITY() as int);";

				var now = _nowProvider.UtcNow;
				var scalar = await _db.ExecuteScalar(sql, [
					new SqlParameter("@ExpenseDate", expense.ExpenseDate),
					new SqlParameter("@Expense", expense.Expense),
					new SqlParameter("@Amount", expense.Amount),
					new SqlParameter("@PaymentMethod", (object?)expense.PaymentMethod ?? DBNull.Value),
					new SqlParameter("@Category", (object?)expense.Category ?? DBNull.Value),
					new SqlParameter("@DatePaid", (object?)expense.DatePaid ?? DBNull.Value),
					new SqlParameter("@UserId", userId),
					new SqlParameter("@CreatedDate", now),
					new SqlParameter("@ModifiedDate", now)
				]);

				return scalar != null ? Convert.ToInt32(scalar) : 0;
			}

			// Optimistic concurrency: only update if ModifiedDate matches
			var updateSql = @"UPDATE Expenses 
				SET ExpenseDate = @ExpenseDate, Expense = @Expense, Amount = @Amount, 
					PaymentMethod = @PaymentMethod, Category = @Category, DatePaid = @DatePaid, 
					ModifiedDate = @ModifiedDate
				WHERE Expense_I = @Id AND UserId = @UserId AND ModifiedDate = @ExpectedModifiedDate";

			var parameters = new List<SqlParameter>
			{
				new SqlParameter("@Id", expense.Expense_I),
				new SqlParameter("@ExpenseDate", expense.ExpenseDate),
				new SqlParameter("@Expense", expense.Expense),
				new SqlParameter("@Amount", expense.Amount),
				new SqlParameter("@PaymentMethod", (object?)expense.PaymentMethod ?? DBNull.Value),
				new SqlParameter("@Category", (object?)expense.Category ?? DBNull.Value),
				new SqlParameter("@DatePaid", (object?)expense.DatePaid ?? DBNull.Value),
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
			parameters.Add(new SqlParameter("@ModifiedDate", _nowProvider.UtcNow));

			var whereConcurrency = "";
			if (expectedModifiedDateTime.HasValue)
			{
				whereConcurrency = " AND ModifiedDate = @ExpectedModifiedDate";
				parameters.Add(new SqlParameter("@ExpectedModifiedDate", expectedModifiedDateTime.Value));
			}
			var sql = $"UPDATE Expenses SET {string.Join(", ", setClauses)} WHERE Expense_I = @Id AND UserId = @UserId{whereConcurrency}";

			var rowsAffected = await _db.ExecuteNonQuery(sql, parameters);
			return rowsAffected > 0;
		}
	}
}
