using Microsoft.Data.SqlClient;
using MoneyManager.Core.Expenses;

namespace MoneyManager.Data.Expenses
{
	public class ExpensePatchDbUpdateBuilder : IExpensePatchDbUpdateBuilder
	{
		public bool AppendPatchSetClauses(
			IReadOnlyDictionary<string, object?> updates,
			ICollection<string> setClauses,
			ICollection<SqlParameter> parameters)
		{
			var added = 0;

			if (updates.ContainsKey(ExpenseFieldNames.ExpenseDate) && updates[ExpenseFieldNames.ExpenseDate] != null)
			{
				setClauses.Add("ExpenseDate = @ExpenseDate");
				parameters.Add(new SqlParameter("@ExpenseDate", updates[ExpenseFieldNames.ExpenseDate]));
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.Expense) && updates[ExpenseFieldNames.Expense] != null)
			{
				setClauses.Add("Expense = @Expense");
				parameters.Add(new SqlParameter("@Expense", updates[ExpenseFieldNames.Expense]));
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.Amount) && updates[ExpenseFieldNames.Amount] != null)
			{
				setClauses.Add("Amount = @Amount");
				parameters.Add(new SqlParameter("@Amount", updates[ExpenseFieldNames.Amount]));
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.Currency) && updates[ExpenseFieldNames.Currency] != null)
			{
				setClauses.Add("Currency = @Currency");
				parameters.Add(new SqlParameter("@Currency", updates[ExpenseFieldNames.Currency]));
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.PaymentMethod))
			{
				if (updates[ExpenseFieldNames.PaymentMethod] == null)
					setClauses.Add("PaymentMethod = NULL");
				else
				{
					setClauses.Add("PaymentMethod = @PaymentMethod");
					parameters.Add(new SqlParameter("@PaymentMethod", updates[ExpenseFieldNames.PaymentMethod]));
				}
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.Category))
			{
				if (updates[ExpenseFieldNames.Category] == null)
					setClauses.Add("Category = NULL");
				else
				{
					setClauses.Add("Category = @Category");
					parameters.Add(new SqlParameter("@Category", updates[ExpenseFieldNames.Category]));
				}
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.DatePaid))
			{
				if (updates[ExpenseFieldNames.DatePaid] == null)
					setClauses.Add("DatePaid = NULL");
				else
				{
					setClauses.Add("DatePaid = @DatePaid");
					parameters.Add(new SqlParameter("@DatePaid", updates[ExpenseFieldNames.DatePaid]));
				}
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.IsSplit))
			{
				setClauses.Add("IsSplit = @IsSplit");
				parameters.Add(new SqlParameter("@IsSplit", updates[ExpenseFieldNames.IsSplit] is bool b && b));
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.ExcludeFromCredit))
			{
				setClauses.Add("ExcludeFromCredit = @ExcludeFromCredit");
				parameters.Add(new SqlParameter("@ExcludeFromCredit", updates[ExpenseFieldNames.ExcludeFromCredit] is bool exclude && exclude));
				added++;
			}

			return added > 0;
		}

		public bool AppendBulkSetClauses(
			IReadOnlyDictionary<string, object?> updates,
			ICollection<string> setClauses,
			ICollection<SqlParameter> parameters)
		{
			var added = 0;

			if (updates.ContainsKey(ExpenseFieldNames.ExpenseDate) && updates[ExpenseFieldNames.ExpenseDate] != null)
			{
				setClauses.Add("ExpenseDate = @ExpenseDate");
				parameters.Add(new SqlParameter("@ExpenseDate", updates[ExpenseFieldNames.ExpenseDate]));
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.Category))
			{
				if (updates[ExpenseFieldNames.Category] == null)
					setClauses.Add("Category = NULL");
				else
				{
					setClauses.Add("Category = @Category");
					parameters.Add(new SqlParameter("@Category", updates[ExpenseFieldNames.Category]));
				}
				added++;
			}
			if (updates.ContainsKey(ExpenseFieldNames.DatePaid))
			{
				if (updates[ExpenseFieldNames.DatePaid] == null)
					setClauses.Add("DatePaid = NULL");
				else
				{
					setClauses.Add("DatePaid = @DatePaid");
					parameters.Add(new SqlParameter("@DatePaid", updates[ExpenseFieldNames.DatePaid]));
				}
				added++;
			}

			return added > 0;
		}
	}
}
