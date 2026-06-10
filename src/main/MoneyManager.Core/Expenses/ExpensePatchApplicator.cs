using MoneyManager.Core.Models;

namespace MoneyManager.Core.Expenses
{
	public static class ExpensePatchApplicator
	{
		public static Expense Apply(Expense current, IReadOnlyDictionary<string, object?> updates, DateTime modifiedUtc)
		{
			var patched = new Expense
			{
				Expense_I = current.Expense_I,
				ExpenseDate = current.ExpenseDate,
				ExpenseDescription = current.ExpenseDescription,
				Amount = current.Amount,
				Currency = current.Currency,
				PaymentMethod = current.PaymentMethod,
				Category = current.Category,
				DatePaid = current.DatePaid,
				CreatedDateTime = current.CreatedDateTime,
				ModifiedDateTime = modifiedUtc,
				IsSplit = current.IsSplit,
				ExcludeFromCredit = current.ExcludeFromCredit,
				CreatedBy = current.CreatedBy
			};
			ApplyTo(patched, updates, modifiedUtc);
			return patched;
		}

		public static void ApplyTo(Expense target, IReadOnlyDictionary<string, object?> updates, DateTime modifiedUtc)
		{
			if (updates.TryGetValue(ExpenseFieldNames.ExpenseDate, out var expenseDate) && expenseDate is DateTime dt)
				target.ExpenseDate = dt;
			if (updates.TryGetValue(ExpenseFieldNames.Expense, out var description) && description is string desc)
				target.ExpenseDescription = desc;
			if (updates.TryGetValue(ExpenseFieldNames.Amount, out var amount) && amount is decimal amt)
				target.Amount = amt;
			if (updates.TryGetValue(ExpenseFieldNames.Currency, out var currency) && currency is string cur)
				target.Currency = cur;
			if (updates.ContainsKey(ExpenseFieldNames.PaymentMethod))
				target.PaymentMethod = (int?)updates[ExpenseFieldNames.PaymentMethod];
			if (updates.ContainsKey(ExpenseFieldNames.Category))
				target.Category = (int?)updates[ExpenseFieldNames.Category];
			if (updates.ContainsKey(ExpenseFieldNames.DatePaid))
				target.DatePaid = (DateTime?)updates[ExpenseFieldNames.DatePaid];
			if (updates.TryGetValue(ExpenseFieldNames.IsSplit, out var isSplit) && isSplit is bool isSplitVal)
				target.IsSplit = isSplitVal;
			if (updates.TryGetValue(ExpenseFieldNames.ExcludeFromCredit, out var exclude) && exclude is bool excludeVal)
				target.ExcludeFromCredit = excludeVal;
			target.ModifiedDateTime = modifiedUtc;
		}
	}
}
