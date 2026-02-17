using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.UseCases.Expenses;

namespace MoneyManager.Core.Repositories
{
	public interface IExpenseRepository
	{
		Task<Expense?> Get(int id, Guid userId);
		Task<IReadOnlyList<Expense>> ListForUser(Guid userId, string? month = null);
		Task<IReadOnlyList<Expense>> ListForUserWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null);
		Task<Expense?> Create(Guid userId, CreateExpenseModel model);
		Task<UpdateExpenseResult> Update(int id, Guid userId, Expense expense);
		Task<UpdateExpenseResult> Patch(int id, Guid userId, Dictionary<string, object?> updates, DateTime? expectedModifiedDateTime);
		Task<bool> Delete(int id, Guid userId);
		Task<bool> BulkUpdate(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates);
		Task<bool> BulkDelete(IEnumerable<int> ids, Guid userId);
	}
}
