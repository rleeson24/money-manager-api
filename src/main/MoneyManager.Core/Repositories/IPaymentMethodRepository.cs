using System.Collections.Generic;
using MoneyManager.Core.Models;

namespace MoneyManager.Core.Repositories
{
	public interface IPaymentMethodRepository
	{
		Task<IReadOnlyList<PaymentMethod>> GetAll();
	}
}
