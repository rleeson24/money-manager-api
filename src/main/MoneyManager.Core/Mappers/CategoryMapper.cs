using MoneyManager.Core.Models;
using MoneyManager.Data.Models;

namespace MoneyManager.Core.Mappers
{
	public interface ICategoryMapper
	{
		Category DbToOutput(DbCategory dbCategory);
	}

	public class CategoryMapper : ICategoryMapper
	{
		public Category DbToOutput(DbCategory dbCategory)
		{
			return new Category
			{
				Category_I = dbCategory.Category_I,
				Name = dbCategory.Name
			};
		}
	}
}
