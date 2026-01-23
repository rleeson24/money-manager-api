using MoneyManager.Core.Models;
using MoneyManager.Core.Mappers;
using MoneyManager.Data.Repositories;
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
		private readonly IPaymentMethodMapper _mapper;

		public GetPaymentMethodsUseCase(IPaymentMethodRepository repository, ILogger<GetPaymentMethodsUseCase> logger, IPaymentMethodMapper mapper)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public async Task<IEnumerable<PaymentMethod>?> Execute()
		{
			try
			{
				var paymentMethods = await _repository.GetAll();
				return paymentMethods.Select(pm => _mapper.DbToOutput(pm));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching payment methods");
				return null;
			}
		}
	}
}
