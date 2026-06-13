using System.Data.Common;
using MoneyManager.Data.Models;

namespace MoneyManager.Data.Mappers
{
	public interface IExpenseMapper
	{
		ValueTask<DbExpense> FromDbReader(DbDataReader reader);
	}
}
