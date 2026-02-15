using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.PaymentMethods
{
	public interface IGetPaymentMethodsUseCase
	{
		Task<IEnumerable<PaymentMethod>?> Execute();
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

		public async Task<IEnumerable<PaymentMethod>?> Execute()
		{
			try
			{
				return await _repository.GetAll();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching payment methods");
				return null;
			}
		}
	}
}
