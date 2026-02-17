using MoneyManager.Core;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Data.Models;

namespace MoneyManager.Data.Mappers
{
	/// <summary>
	/// Maps between Core domain types and Data persistence types (used only within Data layer).
	/// </summary>
	public class ExpenseDomainMapper
	{
		private readonly INowProvider _nowProvider;

		public ExpenseDomainMapper(INowProvider nowProvider)
		{
			_nowProvider = nowProvider;
		}

		internal MoneyManager.Core.Models.Expense ToExpense(DbExpense db)
		{
			return new MoneyManager.Core.Models.Expense
			{
				Expense_I = db.Expense_I,
				ExpenseDate = db.ExpenseDate,
				ExpenseDescription = db.Expense,
				Amount = db.Amount,
				PaymentMethod = db.PaymentMethod,
				Category = db.Category,
				DatePaid = db.DatePaid,
				CreatedDateTime = db.CreatedDate,
				ModifiedDateTime = db.ModifiedDate
			};
		}

		internal DbExpense ToDbExpense(CreateExpenseModel model, Guid userId)
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
				CreatedDate = _nowProvider.UtcNow
			};
		}

		internal void Update(DbExpense existing, CreateExpenseModel model)
		{
			existing.ExpenseDate = model.ExpenseDate;
			existing.Expense = model.Expense;
			existing.Amount = model.Amount;
			existing.PaymentMethod = model.PaymentMethod;
			existing.Category = model.Category;
			existing.DatePaid = model.DatePaid;
			existing.ModifiedDate = _nowProvider.UtcNow;
		}

		internal void Update(DbExpense existing, MoneyManager.Core.Models.Expense expense)
		{
			existing.ExpenseDate = expense.ExpenseDate;
			existing.Expense = expense.ExpenseDescription;
			existing.Amount = expense.Amount;
			existing.PaymentMethod = expense.PaymentMethod;
			existing.Category = expense.Category;
			existing.DatePaid = expense.DatePaid;
			existing.ModifiedDate = _nowProvider.UtcNow;
		}
	}
}
