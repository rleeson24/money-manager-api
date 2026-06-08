using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.PaymentMethods.Queries
{
	public record GetPaymentMethodsQuery : IRequest<IReadOnlyList<PaymentMethod>?>;

	public class GetPaymentMethodsHandler : IRequestHandler<GetPaymentMethodsQuery, IReadOnlyList<PaymentMethod>?>
	{
		private readonly IPaymentMethodRepository _repository;
		private readonly ILogger<GetPaymentMethodsHandler> _logger;

		public GetPaymentMethodsHandler(IPaymentMethodRepository repository, ILogger<GetPaymentMethodsHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<PaymentMethod>?> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
		{
			var paymentMethods = await _repository.GetAll();
			_logger.LogDebug("Fetched {Count} payment methods", paymentMethods.Count);
			return paymentMethods;
		}
	}
}
