using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Core.Utilities;

namespace MoneyManager.Data.Repositories
{
	public class InMemoryCategoryRepository : ICategoryRepository
	{
		private readonly InMemoryStore _store;

		public InMemoryCategoryRepository(InMemoryStore store)
		{
			_store = store;
		}

		public Task<IReadOnlyList<Category>> GetAll(bool activeOnly = false)
		{
			var list = _store.GetCategories();
			if (activeOnly)
				list = list.Where(c => !c.Archived).ToList();
			return Task.FromResult(list);
		}

		public Task<Category?> GetById(int id) =>
			Task.FromResult(_store.GetCategoryById(id));

		public Task<CategoryMutationResult> Create(CreateCategoryModel model) =>
			Task.FromResult(_store.CreateCategory(model));

		public Task<CategoryMutationResult> Update(int id, UpdateCategoryModel model) =>
			Task.FromResult(_store.UpdateCategory(id, model));

		public Task<CategoryDeleteResult> Delete(int id) =>
			Task.FromResult(_store.DeleteCategory(id));

		public Task<bool> IsInUse(int categoryId) =>
			Task.FromResult(_store.IsCategoryInUse(categoryId));
	}
}
