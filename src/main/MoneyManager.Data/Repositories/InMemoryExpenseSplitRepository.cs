using System;
using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;

namespace MoneyManager.Data.Repositories
{
	public class InMemoryExpenseSplitRepository : IExpenseSplitRepository
	{
		private readonly InMemoryStore _store;

		public InMemoryExpenseSplitRepository(InMemoryStore store)
		{
			_store = store;
		}

		public Task<IReadOnlyList<ExpenseSplit>> GetByExpenseId(int expense_I, Guid userId) =>
			Task.FromResult(_store.GetSplitsByExpenseId(expense_I));

		public Task<ExpenseSplit?> Get(int id, Guid userId) =>
			Task.FromResult(_store.GetSplitById(id));

		public Task<ExpenseSplit?> Create(Guid userId, CreateOrUpdateExpenseSplitModel model)
		{
			var split = new ExpenseSplit
			{
				Id = 0,
				Expense_I = model.Expense_I,
				Description = model.Description,
				Amount = model.Amount,
				Category = model.Category,
				CreatedDateTime = DateTime.UtcNow
			};
			var added = _store.AddSplit(split);
			return Task.FromResult<ExpenseSplit?>(added);
		}

		public Task<ExpenseSplit?> Update(int id, Guid userId, CreateOrUpdateExpenseSplitModel model)
		{
			var existing = _store.GetSplitById(id);
			if (existing == null) return Task.FromResult<ExpenseSplit?>(null);
			var updated = new ExpenseSplit
			{
				Id = existing.Id,
				Expense_I = model.Expense_I,
				Description = model.Description,
				Amount = model.Amount,
				Category = model.Category,
				CreatedDateTime = existing.CreatedDateTime
			};
			_store.UpdateSplit(id, updated);
			return Task.FromResult<ExpenseSplit?>(updated);
		}

		public Task<bool> Delete(int id, Guid userId) =>
			Task.FromResult(_store.RemoveSplit(id));

		public Task<ReplaceSplitsResult> ReplaceByExpenseId(int expense_I, Guid userId, decimal parentAmount, IReadOnlyList<ReplaceExpenseSplitItemModel> items)
		{
			var sum = items.Aggregate(0m, (a, i) => a + i.Amount);
			if (items.Count > 0 && Math.Abs(sum - parentAmount) > 0.005m)
				return Task.FromResult(ReplaceSplitsResult.Failure("Split amounts must add up to the expense total."));

			_store.RemoveSplitsByExpenseId(expense_I);
			var now = DateTime.UtcNow;
			var created = new List<ExpenseSplit>();
			foreach (var item in items)
			{
				var split = _store.AddSplit(new ExpenseSplit
				{
					Id = 0,
					Expense_I = expense_I,
					Description = item.Description,
					Amount = item.Amount,
					Category = item.Category,
					CreatedDateTime = now
				});
				created.Add(split);
			}
			return Task.FromResult(ReplaceSplitsResult.Success(created));
		}
	}
}
