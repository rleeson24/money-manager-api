using MoneyManager.Data.Models;

namespace MoneyManager.Data.Repositories
{
	public interface IExpenseRepository
	{
		Task<DbExpense?> Get(int id, Guid userId);
		Task<IEnumerable<DbExpense>> ListForUser(Guid userId, string? month = null);
		Task<int> Save(Guid userId, DbExpense expense);
		Task<bool> Delete(int id, Guid userId);
		Task<bool> BulkUpdate(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates);
		Task<bool> BulkDelete(IEnumerable<int> ids, Guid userId);
		Task<bool> Update(int id, Guid userId, Dictionary<string, object?> updates);
	}
}
