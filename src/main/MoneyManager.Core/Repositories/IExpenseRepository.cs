using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.Core.Repositories
{
	public interface IExpenseRepository
	{
		Task<Expense?> Get(int id, Guid userId);
		Task<IReadOnlyList<Expense>> ListForUser(Guid userId, string? month = null);
		Task<IReadOnlyList<Expense>> ListForUserWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null);
		Task<Expense?> Create(Guid userId, CreateExpenseModel model);
		Task<Expense?> Update(int id, Guid userId, Expense expense);
		Task<Expense?> Patch(int id, Guid userId, Dictionary<string, object?> updates);
		Task<bool> Delete(int id, Guid userId);
		Task<bool> BulkUpdate(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates);
		Task<bool> BulkDelete(IEnumerable<int> ids, Guid userId);
	}
}
