using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.PaymentMethods
{
	public interface IGetPaymentMethodsUseCase
	{
		Task<IReadOnlyList<PaymentMethod>?> Execute();
	}

	public class GetPaymentMethodsUseCase : IGetPaymentMethodsUseCase
	{
		private readonly IPaymentMethodRepository _repository;
		private readonly ILogger<GetPaymentMethodsUseCase> _logger;

		public GetPaymentMethodsUseCase(IPaymentMethodRepository repository, ILogger<GetPaymentMethodsUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<PaymentMethod>?> Execute()
		{
			try
			{
				var paymentMethods = await _repository.GetAll();
				_logger.LogDebug("Fetched {Count} payment methods", paymentMethods.Count);
				return paymentMethods;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch payment methods");
				return null;
			}
		}
	}
}
