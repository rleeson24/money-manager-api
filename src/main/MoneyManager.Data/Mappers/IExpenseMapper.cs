using MoneyManager.Data.Models;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Mappers
{
	public interface IExpenseMapper
	{
		ValueTask<DbExpense> FromDbReader(SqlDataReader reader);
	}
}
