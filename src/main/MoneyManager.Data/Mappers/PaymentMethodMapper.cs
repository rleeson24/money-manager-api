using MoneyManager.Data.Models;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Mappers
{
	public interface IPaymentMethodMapper
	{
		Task<DbPaymentMethod> FromDbReader(SqlDataReader reader);
	}

	public class PaymentMethodMapper : IPaymentMethodMapper
	{
		public async Task<DbPaymentMethod> FromDbReader(SqlDataReader reader)
		{
			return new DbPaymentMethod
			{
				ID = reader.GetInt32(reader.GetOrdinal("ID")),
				PaymentMethod = reader.GetString(reader.GetOrdinal("PaymentMethod"))
			};
		}
	}
}
