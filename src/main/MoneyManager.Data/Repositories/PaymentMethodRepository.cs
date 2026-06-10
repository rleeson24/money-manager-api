using System.Collections.Generic;
using System.Linq;
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

		public Task<IReadOnlyList<PaymentMethod>> GetAll() =>
			GetAllFromDb();

		private async Task<IReadOnlyList<PaymentMethod>> GetAllFromDb()
		{
			var result = new List<DbPaymentMethod>();
			await _db.ExecuteReader("SELECT * FROM PaymentMethods ORDER BY PaymentMethod", [],
				async sqlReader =>
				{
					while (await sqlReader.ReadAsync())
						result.Add(await _readerMapper.FromDbReader(sqlReader));
				});
			return result
				.GroupBy(db => db.PaymentMethod, StringComparer.OrdinalIgnoreCase)
				.Select(g => g.OrderBy(x => x.ID).First())
				.OrderBy(db => db.PaymentMethod)
				.Select(db => new PaymentMethod
				{
					ID = db.ID,
					PaymentMethodName = db.PaymentMethod
				})
				.ToList();
		}
	}
}
