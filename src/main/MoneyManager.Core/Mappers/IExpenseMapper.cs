using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Data.Models;

namespace MoneyManager.Core.Mappers
{
	public interface IExpenseMapper
	{
		Expense DbToOutput(DbExpense dbExpense);
		DbExpense Create(CreateExpenseModel model, Guid userId);
		DbExpense Update(DbExpense existing, CreateExpenseModel model);
	}
}
