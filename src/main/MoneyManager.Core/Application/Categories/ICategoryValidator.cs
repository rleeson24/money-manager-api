using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.Core.Application.Categories
{
	public interface ICategoryValidator
	{
		string? ValidateCreate(CreateCategoryModel model, IReadOnlyList<Category> existing);

		string? ValidateUpdate(Category current, UpdateCategoryModel model, IReadOnlyList<Category> existing);

		string? ValidateDelete(Category current, IReadOnlyList<Category> existing, bool inUse);
	}
}
