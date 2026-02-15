using MoneyManager.Core.Models;

namespace MoneyManager.Core.Repositories
{
	public interface IPaymentMethodRepository
	{
		Task<IEnumerable<PaymentMethod>> GetAll();
	}
}
