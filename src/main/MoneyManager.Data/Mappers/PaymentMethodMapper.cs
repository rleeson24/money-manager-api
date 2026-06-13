using System.Data.Common;
using MoneyManager.Data.Models;

namespace MoneyManager.Data.Mappers
{
	public interface IPaymentMethodMapper
	{
		Task<DbPaymentMethod> FromDbReader(DbDataReader reader);
	}

	public class PaymentMethodMapper : IPaymentMethodMapper
	{
		public async Task<DbPaymentMethod> FromDbReader(DbDataReader reader)
		{
			return new DbPaymentMethod
			{
				ID = reader.GetInt32(reader.GetOrdinal("ID")),
				PaymentMethod = reader.GetString(reader.GetOrdinal("PaymentMethod"))
			};
		}
	}
}
