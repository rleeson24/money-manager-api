using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.ExpenseSplits.Queries
{
	public record GetExpenseSplitsQuery(int ExpenseId, Guid UserId) : IRequest<IReadOnlyList<ExpenseSplit>>;

	public class GetExpenseSplitsHandler : IRequestHandler<GetExpenseSplitsQuery, IReadOnlyList<ExpenseSplit>>
	{
		private readonly IExpenseSplitRepository _repository;
		private readonly ILogger<GetExpenseSplitsHandler> _logger;

		public GetExpenseSplitsHandler(IExpenseSplitRepository repository, ILogger<GetExpenseSplitsHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<ExpenseSplit>> Handle(GetExpenseSplitsQuery request, CancellationToken cancellationToken)
		{
			var splits = await _repository.GetByExpenseId(request.ExpenseId, request.UserId);
			_logger.LogDebug("Fetched {Count} splits for expense {ExpenseId}, user {UserId}", splits.Count, request.ExpenseId, request.UserId);
			return splits;
		}
	}
}
