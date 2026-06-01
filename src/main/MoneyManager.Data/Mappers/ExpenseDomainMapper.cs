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
				Currency = db.Currency,
				PaymentMethod = db.PaymentMethod,
				Category = db.Category,
				DatePaid = db.DatePaid,
				CreatedDateTime = db.CreatedDate,
				ModifiedDateTime = db.ModifiedDate,
				IsSplit = db.IsSplit,
				ExcludeFromCredit = db.ExcludeFromCredit,
				CreatedBy = db.CreatedBy
			};
		}

		internal DbExpense ToDbExpense(CreateExpenseModel model, Guid userId)
		{
			return new DbExpense
			{
				ExpenseDate = model.ExpenseDate,
				Expense = model.Expense,
				Amount = model.Amount,
				Currency = string.IsNullOrWhiteSpace(model.Currency) ? "USD" : model.Currency,
				PaymentMethod = model.PaymentMethod,
				Category = model.Category,
				DatePaid = model.DatePaid,
				UserId = userId,
				CreatedDate = _nowProvider.UtcNow,
				IsSplit = model.IsSplit,
				ExcludeFromCredit = model.ExcludeFromCredit,
				CreatedBy = model.CreatedBy ?? userId.ToString()
			};
		}

		internal void Update(DbExpense existing, CreateExpenseModel model)
		{
			existing.ExpenseDate = model.ExpenseDate;
			existing.Expense = model.Expense;
			existing.Amount = model.Amount;
			existing.Currency = string.IsNullOrWhiteSpace(model.Currency) ? "USD" : model.Currency;
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
			existing.Currency = string.IsNullOrWhiteSpace(expense.Currency) ? "USD" : expense.Currency;
			existing.PaymentMethod = expense.PaymentMethod;
			existing.Category = expense.Category;
			existing.DatePaid = expense.DatePaid;
			existing.ModifiedDate = _nowProvider.UtcNow;
			existing.IsSplit = expense.IsSplit;
			existing.ExcludeFromCredit = expense.ExcludeFromCredit;
		}
	}
}
