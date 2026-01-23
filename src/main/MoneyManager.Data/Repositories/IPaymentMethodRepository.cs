using MoneyManager.Data.Models;

namespace MoneyManager.Data.Repositories
{
	public interface IPaymentMethodRepository
	{
		Task<IEnumerable<DbPaymentMethod>> GetAll();
	}
}
