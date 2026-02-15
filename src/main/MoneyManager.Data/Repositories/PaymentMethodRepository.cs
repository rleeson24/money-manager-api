using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;

namespace MoneyManager.Data.Repositories
{
	public class PaymentMethodRepository : IPaymentMethodRepository
	{
		private readonly DbExecutor _db;
		private readonly IPaymentMethodMapper _readerMapper;

		public PaymentMethodRepository(DbExecutor db, IPaymentMethodMapper readerMapper)
		{
			_db = db;
			_readerMapper = readerMapper;
		}

		public async Task<IEnumerable<PaymentMethod>> GetAll()
		{
			var result = new List<DbPaymentMethod>();
			await _db.ExecuteReader("SELECT * FROM PaymentMethods ORDER BY PaymentMethod", [],
				async sqlReader =>
				{
					while (await sqlReader.ReadAsync())
						result.Add(await _readerMapper.FromDbReader(sqlReader));
				});
			return result.Select(db => new PaymentMethod
			{
				ID = db.ID,
				PaymentMethodName = db.PaymentMethod
			});
		}
	}
}
