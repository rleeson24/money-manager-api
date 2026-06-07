using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Import.Queries
{
	public record GetLastImportDatesQuery(Guid UserId, IReadOnlyList<int> PaymentMethodIds) : IRequest<IReadOnlyList<LastImportDatesForPaymentMethod>>;

	public class GetLastImportDatesHandler : IRequestHandler<GetLastImportDatesQuery, IReadOnlyList<LastImportDatesForPaymentMethod>>
	{
		private readonly IExpenseRepository _expenseRepository;
		private readonly ILogger<GetLastImportDatesHandler> _logger;

		public GetLastImportDatesHandler(IExpenseRepository expenseRepository, ILogger<GetLastImportDatesHandler> logger)
		{
			_expenseRepository = expenseRepository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<LastImportDatesForPaymentMethod>> Handle(GetLastImportDatesQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var results = await _expenseRepository.GetLastImportDates(request.UserId, request.PaymentMethodIds);
				_logger.LogDebug(
					"Fetched last import dates for user {UserId} across {PaymentMethodCount} payment methods",
					request.UserId, request.PaymentMethodIds.Count);
				return results;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch last import dates for user {UserId}", request.UserId);
				throw;
			}
		}
	}
}
