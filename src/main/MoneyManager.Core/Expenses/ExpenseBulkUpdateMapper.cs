using MoneyManager.Core.Models.Input;

namespace MoneyManager.Core.Expenses
{
	public class ExpenseBulkUpdateMapper : IExpenseBulkUpdateMapper
	{
		public Dictionary<string, object?> ToUpdates(BulkUpdateRequest request)
		{
			var updates = new Dictionary<string, object?>();
			if (request.ExpenseDate != null)
				updates[ExpenseFieldNames.ExpenseDate] = request.ExpenseDate;
			if (request.SetCategoryToNull == true)
				updates[ExpenseFieldNames.Category] = null;
			else if (request.Category != null)
				updates[ExpenseFieldNames.Category] = request.Category;
			if (request.SetDatePaidToNull == true)
				updates[ExpenseFieldNames.DatePaid] = null;
			else if (request.DatePaid != null)
				updates[ExpenseFieldNames.DatePaid] = request.DatePaid;
			return updates;
		}
	}
}
