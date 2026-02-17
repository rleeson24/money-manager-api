using System.Collections.Generic;
using MoneyManager.Core.Models;

namespace MoneyManager.Core.Repositories
{
	public interface ICategoryRepository
	{
		Task<IReadOnlyList<Category>> GetAll();
	}
}
