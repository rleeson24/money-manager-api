using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;

namespace MoneyManager.Data.Repositories
{
	public class InMemoryCategoryRepository : ICategoryRepository
	{
		private readonly InMemoryStore _store;

		public InMemoryCategoryRepository(InMemoryStore store)
		{
			_store = store;
		}

		public Task<IReadOnlyList<Category>> GetAll() =>
			Task.FromResult(_store.Categories);
	}
}
