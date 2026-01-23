using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Data.Models;

namespace MoneyManager.Core.Mappers
{
	public class ExpenseMapper : IExpenseMapper
	{
		public Expense DbToOutput(DbExpense dbExpense)
		{
			return new Expense
			{
				Expense_I = dbExpense.Expense_I,
				ExpenseDate = dbExpense.ExpenseDate,
				ExpenseDescription = dbExpense.Expense,
				Amount = dbExpense.Amount,
				PaymentMethod = dbExpense.PaymentMethod,
				Category = dbExpense.Category,
				DatePaid = dbExpense.DatePaid
			};
		}

		public DbExpense Create(CreateExpenseModel model, Guid userId)
		{
			return new DbExpense
			{
				ExpenseDate = model.ExpenseDate,
				Expense = model.Expense,
				Amount = model.Amount,
				PaymentMethod = model.PaymentMethod,
				Category = model.Category,
				DatePaid = model.DatePaid,
				UserId = userId,
				CreatedDate = DateTime.UtcNow
			};
		}

		public DbExpense Update(DbExpense existing, CreateExpenseModel model)
		{
			existing.ExpenseDate = model.ExpenseDate;
			existing.Expense = model.Expense;
			existing.Amount = model.Amount;
			existing.PaymentMethod = model.PaymentMethod;
			existing.Category = model.Category;
			existing.DatePaid = model.DatePaid;
			existing.ModifiedDate = DateTime.UtcNow;
			return existing;
		}
	}
}
