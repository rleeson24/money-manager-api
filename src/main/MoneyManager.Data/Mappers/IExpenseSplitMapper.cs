using MoneyManager.Data.Models;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Mappers
{
	public interface IExpenseSplitMapper
	{
		Task<DbExpenseSplit> FromDbReader(SqlDataReader reader);
	}
}
