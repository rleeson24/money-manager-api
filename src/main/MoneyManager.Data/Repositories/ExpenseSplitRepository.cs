using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Repositories
{
	public class ExpenseSplitRepository : IExpenseSplitRepository
	{
		private readonly DbExecutor _db;
		private readonly IExpenseSplitMapper _mapper;

		public ExpenseSplitRepository(DbExecutor db, IExpenseSplitMapper mapper)
		{
			_db = db;
			_mapper = mapper;
		}

		public Task<IReadOnlyList<ExpenseSplit>> GetByExpenseId(int expense_I, Guid userId) =>
			GetByExpenseIdFromDb(expense_I, userId);

		private async Task<IReadOnlyList<ExpenseSplit>> GetByExpenseIdFromDb(int expense_I, Guid userId)
		{
			var result = new List<DbExpenseSplit>();
			await _db.ExecuteReader(
				"SELECT * FROM Expenses_split WHERE Expense_I = @Expense_I AND UserId = @UserId ORDER BY Id",
				[
					new SqlParameter("@Expense_I", expense_I),
					new SqlParameter("@UserId", userId)
				],
				async reader =>
				{
					while (await reader.ReadAsync())
						result.Add(await _mapper.FromDbReader(reader));
				});
			return result.Select(ToExpenseSplit).ToList();
		}

		public async Task<ExpenseSplit?> Get(int id, Guid userId)
		{
			var result = default(DbExpenseSplit?);
			await _db.ExecuteReader(
				"SELECT * FROM Expenses_split WHERE Id = @Id AND UserId = @UserId",
				[new SqlParameter("@Id", id), new SqlParameter("@UserId", userId)],
				async reader =>
				{
					if (await reader.ReadAsync())
						result = await _mapper.FromDbReader(reader);
				});
			return result != null ? ToExpenseSplit(result) : null;
		}

		public async Task<ExpenseSplit?> Create(Guid userId, CreateOrUpdateExpenseSplitModel model)
		{
			var sql = @"INSERT INTO Expenses_split (Expense_I, UserId, Description, Amount, Category, CreatedDateTime)
				VALUES (@Expense_I, @UserId, @Description, @Amount, @Category, GETUTCDATE());
				SELECT CAST(SCOPE_IDENTITY() AS INT);";
			var scalar = await _db.ExecuteScalar(sql, [
				new SqlParameter("@Expense_I", model.Expense_I),
				new SqlParameter("@UserId", userId),
				new SqlParameter("@Description", model.Description),
				new SqlParameter("@Amount", model.Amount),
				new SqlParameter("@Category", model.Category)
			]);
			var newId = scalar != null ? Convert.ToInt32(scalar) : 0;
			return newId > 0 ? await Get(newId, userId) : null;
		}

		public async Task<ExpenseSplit?> Update(int id, Guid userId, CreateOrUpdateExpenseSplitModel model)
		{
			var rows = await _db.ExecuteNonQuery(
				@"UPDATE Expenses_split SET Description = @Description, Amount = @Amount, Category = @Category
					WHERE Id = @Id AND UserId = @UserId",
				[
					new SqlParameter("@Description", model.Description),
					new SqlParameter("@Amount", model.Amount),
					new SqlParameter("@Category", model.Category),
					new SqlParameter("@Id", id),
					new SqlParameter("@UserId", userId)
				]);
			return rows > 0 ? await Get(id, userId) : null;
		}

		public async Task<bool> Delete(int id, Guid userId)
		{
			var rows = await _db.ExecuteNonQuery(
				"DELETE FROM Expenses_split WHERE Id = @Id AND UserId = @UserId",
				[new SqlParameter("@Id", id), new SqlParameter("@UserId", userId)]);
			return rows > 0;
		}

		public async Task<ReplaceSplitsResult> ReplaceByExpenseId(int expense_I, Guid userId, decimal parentAmount, IReadOnlyList<ReplaceExpenseSplitItemModel> items)
		{
			var sum = items.Aggregate(0m, (a, i) => a + i.Amount);
			if (Math.Abs(sum - parentAmount) > 0.005m)
				return ReplaceSplitsResult.Failure("Split amounts must add up to the expense total.");

			await _db.ExecuteNonQuery(
				"DELETE FROM Expenses_split WHERE Expense_I = @Expense_I AND UserId = @UserId",
				[new SqlParameter("@Expense_I", expense_I), new SqlParameter("@UserId", userId)]);

			foreach (var item in items)
			{
				await _db.ExecuteScalar(
					@"INSERT INTO Expenses_split (Expense_I, UserId, Description, Amount, Category, CreatedDateTime)
					  VALUES (@Expense_I, @UserId, @Description, @Amount, @Category, GETUTCDATE());
					  SELECT CAST(SCOPE_IDENTITY() AS INT);",
					[
						new SqlParameter("@Expense_I", expense_I),
						new SqlParameter("@UserId", userId),
						new SqlParameter("@Description", item.Description),
						new SqlParameter("@Amount", item.Amount),
						new SqlParameter("@Category", item.Category)
					]);
			}
			var result = await GetByExpenseIdFromDb(expense_I, userId);
			return ReplaceSplitsResult.Success(result);
		}

		private static ExpenseSplit ToExpenseSplit(DbExpenseSplit db) =>
			new ExpenseSplit
			{
				Id = db.Id,
				Expense_I = db.Expense_I,
				Description = db.Description,
				Amount = db.Amount,
				Category = db.Category,
				CreatedDateTime = db.CreatedDateTime
			};
	}
}
