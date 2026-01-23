using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;

namespace MoneyManager.Data.Repositories
{
	public class PaymentMethodRepository : IPaymentMethodRepository
	{
		private readonly DbExecutor _db;
		private readonly IPaymentMethodMapper _mapper;

		public PaymentMethodRepository(DbExecutor db, IPaymentMethodMapper mapper)
		{
			_db = db;
			_mapper = mapper;
		}

		public async Task<IEnumerable<DbPaymentMethod>> GetAll()
		{
			var result = new List<DbPaymentMethod>();
			await _db.ExecuteReader("SELECT * FROM PaymentMethods ORDER BY PaymentMethod", [],
				async sqlReader =>
				{
					while (await sqlReader.ReadAsync())
					{
						result.Add(await _mapper.FromDbReader(sqlReader));
					}
				});
			return result;
		}
	}
}
