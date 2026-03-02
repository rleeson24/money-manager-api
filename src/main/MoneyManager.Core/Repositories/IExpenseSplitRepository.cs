using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.Core.Repositories
{
	public interface IExpenseSplitRepository
	{
		Task<IReadOnlyList<ExpenseSplit>> GetByExpenseId(int expense_I, Guid userId);
		Task<ExpenseSplit?> Get(int id, Guid userId);
		Task<ExpenseSplit?> Create(Guid userId, CreateOrUpdateExpenseSplitModel model);
		Task<ExpenseSplit?> Update(int id, Guid userId, CreateOrUpdateExpenseSplitModel model);
		Task<bool> Delete(int id, Guid userId);
		Task<ReplaceSplitsResult> ReplaceByExpenseId(int expense_I, Guid userId, decimal parentAmount, IReadOnlyList<ReplaceExpenseSplitItemModel> items);
	}
}
