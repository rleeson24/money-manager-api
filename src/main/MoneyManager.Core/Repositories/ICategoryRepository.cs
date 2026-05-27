using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.Core.Repositories
{
	public interface ICategoryRepository
	{
		Task<IReadOnlyList<Category>> GetAll(bool activeOnly = false);
		Task<Category?> GetById(int id);
		Task<CategoryMutationResult> Create(CreateCategoryModel model);
		Task<CategoryMutationResult> Update(int id, UpdateCategoryModel model);
		Task<CategoryDeleteResult> Delete(int id);
		Task<bool> IsInUse(int categoryId);
	}
}
