using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;
using Microsoft.Extensions.Options;
using DataOptions = MoneyManager.Data.DataOptions;

namespace MoneyManager.Data.Repositories
{
	public class PaymentMethodRepository : IPaymentMethodRepository
	{
		private readonly DbExecutor _db;
		private readonly IPaymentMethodMapper _readerMapper;
		private readonly DataOptions _dataOptions;

		public PaymentMethodRepository(DbExecutor db, IPaymentMethodMapper readerMapper, IOptions<DataOptions> dataOptions)
		{
			_db = db;
			_readerMapper = readerMapper;
			_dataOptions = dataOptions.Value;
		}

		public Task<IReadOnlyList<PaymentMethod>> GetAll()
		{
			if (_dataOptions.UseMockData)
				return Task.FromResult(MockData.PaymentMethods);

			return GetAllFromDb();
		}

		private async Task<IReadOnlyList<PaymentMethod>> GetAllFromDb()
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
			}).ToList();
		}
	}
}
