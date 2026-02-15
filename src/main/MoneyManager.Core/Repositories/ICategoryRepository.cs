using MoneyManager.Core.Models;

namespace MoneyManager.Core.Repositories
{
	public interface ICategoryRepository
	{
		Task<IEnumerable<Category>> GetAll();
	}
}
