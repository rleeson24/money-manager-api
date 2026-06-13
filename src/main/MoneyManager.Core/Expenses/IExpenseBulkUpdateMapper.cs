using MoneyManager.Core.Models.Input;

namespace MoneyManager.Core.Expenses
{
	public interface IExpenseBulkUpdateMapper
	{
		Dictionary<string, object?> ToUpdates(BulkUpdateRequest request);
	}
}
