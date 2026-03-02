using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;

namespace MoneyManager.Data.Repositories
{
	public class InMemoryPaymentMethodRepository : IPaymentMethodRepository
	{
		private readonly InMemoryStore _store;

		public InMemoryPaymentMethodRepository(InMemoryStore store)
		{
			_store = store;
		}

		public Task<IReadOnlyList<PaymentMethod>> GetAll() =>
			Task.FromResult(_store.PaymentMethods);
	}
}
