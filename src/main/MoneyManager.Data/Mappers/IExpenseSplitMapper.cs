using System.Data.Common;
using MoneyManager.Data.Models;

namespace MoneyManager.Data.Mappers
{
	public interface IExpenseSplitMapper
	{
		Task<DbExpenseSplit> FromDbReader(DbDataReader reader);
	}
}
