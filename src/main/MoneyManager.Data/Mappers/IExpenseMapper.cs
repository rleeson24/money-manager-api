using MoneyManager.Data.Models;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Mappers
{
	public interface IExpenseMapper
	{
		Task<DbExpense> FromDbReader(SqlDataReader reader);
	}
}
