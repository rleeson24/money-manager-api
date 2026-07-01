using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.Core.Repositories
{
	public interface IExpenseRepository
	{
		Task<Expense?> Get(int id, Guid userId);
		Task<IReadOnlyList<Expense>> ListForUser(Guid userId, string? month = null);
		Task<IReadOnlyList<Expense>> ListForUserWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null, string? currency = null);
		Task<Expense?> Create(Guid userId, CreateExpenseModel model);
		Task<UpdateExpenseResult> Update(int id, Guid userId, Expense expense);
		Task<UpdateExpenseResult> Patch(int id, Guid userId, Dictionary<string, object?> updates, DateTime? expectedModifiedDateTime);
		Task<bool> Delete(int id, Guid userId);
		Task<bool> BulkUpdate(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates);
		Task<bool> BulkDelete(IEnumerable<int> ids, Guid userId);
		Task<IReadOnlyList<Expense>> ListForUserInDateRange(Guid userId, DateTime fromDate, DateTime toDate, int? paymentMethodId = null);
		Task<IReadOnlyList<Expense>> SearchForUser(
			Guid userId,
			DateTime fromDate,
			DateTime toDate,
			string? search,
			IReadOnlyList<int>? categoryIds);
		Task<IReadOnlyList<LastImportDatesForPaymentMethod>> GetLastImportDates(Guid userId, IReadOnlyList<int> paymentMethodIds);
	}
}
