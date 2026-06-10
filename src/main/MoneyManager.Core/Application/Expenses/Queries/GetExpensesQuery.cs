using FluentValidation;
using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Queries
{
	public record GetExpensesQuery(
		Guid UserId,
		string? Month = null,
		int? PaymentMethod = null,
		bool? DatePaidNull = null,
		string? Currency = null) : IRequest<IReadOnlyList<Expense>?>;

	public class GetExpensesHandler : IRequestHandler<GetExpensesQuery, IReadOnlyList<Expense>?>
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<GetExpensesHandler> _logger;

		public GetExpensesHandler(IExpenseRepository repository, ILogger<GetExpensesHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<Expense>?> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
		{
			var useFilters = request.PaymentMethod.HasValue
				|| request.DatePaidNull.HasValue
				|| !string.IsNullOrWhiteSpace(request.Currency);

			if (useFilters)
			{
				var expenses = await _repository.ListForUserWithFilters(
					request.UserId, request.PaymentMethod, request.DatePaidNull, request.Currency);
				_logger.LogDebug(
					"Fetched {Count} filtered expenses for user {UserId}",
					expenses.Count, request.UserId);
				return expenses;
			}

			var list = await _repository.ListForUser(request.UserId, request.Month);
			_logger.LogDebug(
				"Fetched {Count} expenses for user {UserId} (month={Month})",
				list.Count, request.UserId, request.Month ?? "all");
			return list;
		}
	}

	public class GetExpensesQueryValidator : AbstractValidator<GetExpensesQuery>
	{
		public GetExpensesQueryValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x)
				.Must(query =>
				{
					var useFilters = query.PaymentMethod.HasValue
						|| query.DatePaidNull.HasValue
						|| !string.IsNullOrWhiteSpace(query.Currency);
					return !(useFilters && !string.IsNullOrWhiteSpace(query.Month));
				})
				.WithMessage("Cannot combine month with payment method, datePaidNull, or currency filters.");
		}
	}
}
