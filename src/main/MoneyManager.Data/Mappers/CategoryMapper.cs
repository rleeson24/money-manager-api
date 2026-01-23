using MoneyManager.Data.Models;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Mappers
{
	public interface ICategoryMapper
	{
		Task<DbCategory> FromDbReader(SqlDataReader reader);
	}

	public class CategoryMapper : ICategoryMapper
	{
		public async Task<DbCategory> FromDbReader(SqlDataReader reader)
		{
			return new DbCategory
			{
				Category_I = reader.GetInt32(reader.GetOrdinal("Category_I")),
				Name = reader.GetString(reader.GetOrdinal("Name"))
			};
		}
	}
}
