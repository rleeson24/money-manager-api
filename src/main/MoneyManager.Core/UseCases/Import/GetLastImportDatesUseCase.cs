using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;

namespace MoneyManager.Core.UseCases.Import
{
	public interface IGetLastImportDatesUseCase
	{
		Task<IReadOnlyList<LastImportDatesForPaymentMethod>> ExecuteAsync(Guid userId, IReadOnlyList<int> paymentMethodIds);
	}

	public class GetLastImportDatesUseCase : IGetLastImportDatesUseCase
	{
		private readonly IExpenseRepository _expenseRepository;

		public GetLastImportDatesUseCase(IExpenseRepository expenseRepository)
		{
			_expenseRepository = expenseRepository;
		}

		public Task<IReadOnlyList<LastImportDatesForPaymentMethod>> ExecuteAsync(Guid userId, IReadOnlyList<int> paymentMethodIds)
		{
			return _expenseRepository.GetLastImportDates(userId, paymentMethodIds);
		}
	}
}
