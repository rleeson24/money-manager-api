using MoneyManager.Core.Models;
using MoneyManager.Data.Models;

namespace MoneyManager.Core.Mappers
{
	public interface IPaymentMethodMapper
	{
		PaymentMethod DbToOutput(DbPaymentMethod dbPaymentMethod);
	}

	public class PaymentMethodMapper : IPaymentMethodMapper
	{
		public PaymentMethod DbToOutput(DbPaymentMethod dbPaymentMethod)
		{
			return new PaymentMethod
			{
				ID = dbPaymentMethod.ID,
				PaymentMethodName = dbPaymentMethod.PaymentMethod
			};
		}
	}
}
