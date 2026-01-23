using MoneyManager.Data.Models;

namespace MoneyManager.Data.Repositories
{
	public interface ICategoryRepository
	{
		Task<IEnumerable<DbCategory>> GetAll();
	}
}
