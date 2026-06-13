using System.Data.Common;
using MoneyManager.Data.Models;

namespace MoneyManager.Data.Mappers
{
	public class ExpenseSplitMapper : IExpenseSplitMapper
	{
		public async Task<DbExpenseSplit> FromDbReader(DbDataReader reader)
		{
			return await Task.FromResult(new DbExpenseSplit
			{
				Id = reader.GetInt32(reader.GetOrdinal("Id")),
				Expense_I = reader.GetInt32(reader.GetOrdinal("Expense_I")),
				UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
				Description = reader.GetString(reader.GetOrdinal("Description")),
				Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
				Category = reader.GetInt32(reader.GetOrdinal("Category")),
				CreatedDateTime = reader.GetDateTime(reader.GetOrdinal("CreatedDateTime"))
			});
		}
	}
}
