using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Import
{
	public interface IGetLastImportDatesUseCase
	{
		Task<IReadOnlyList<LastImportDatesForPaymentMethod>> ExecuteAsync(Guid userId, IReadOnlyList<int> paymentMethodIds);
	}

	public class GetLastImportDatesUseCase : IGetLastImportDatesUseCase
	{
		private readonly IExpenseRepository _expenseRepository;
		private readonly ILogger<GetLastImportDatesUseCase> _logger;

		public GetLastImportDatesUseCase(IExpenseRepository expenseRepository, ILogger<GetLastImportDatesUseCase> logger)
		{
			_expenseRepository = expenseRepository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<LastImportDatesForPaymentMethod>> ExecuteAsync(Guid userId, IReadOnlyList<int> paymentMethodIds)
		{
			try
			{
				var results = await _expenseRepository.GetLastImportDates(userId, paymentMethodIds);
				_logger.LogDebug(
					"Fetched last import dates for user {UserId} across {PaymentMethodCount} payment methods",
					userId, paymentMethodIds.Count);
				return results;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch last import dates for user {UserId}", userId);
				throw;
			}
		}
	}
}
