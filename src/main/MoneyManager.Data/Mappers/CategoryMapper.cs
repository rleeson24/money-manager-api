using System.Data.Common;
using MoneyManager.Data.Models;

namespace MoneyManager.Data.Mappers
{
	public interface ICategoryMapper
	{
		Task<DbCategory> FromDbReader(DbDataReader reader);
	}

	public class CategoryMapper : ICategoryMapper
	{
		public Task<DbCategory> FromDbReader(DbDataReader reader)
		{
			var parentOrdinal = reader.GetOrdinal("ParentCategory_I");
			return Task.FromResult(new DbCategory
			{
				Category_I = reader.GetInt32(reader.GetOrdinal("Category_I")),
				Name = reader.GetString(reader.GetOrdinal("Name")),
				ParentCategory_I = reader.IsDBNull(parentOrdinal) ? null : reader.GetInt32(parentOrdinal),
				Required = reader.GetBoolean(reader.GetOrdinal("Required")),
				Archived = reader.GetBoolean(reader.GetOrdinal("Archived"))
			});
		}
	}
}
