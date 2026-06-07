using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Queries
{
	public record GetExpenseQuery(int Id, Guid UserId) : IRequest<Expense?>;

	public class GetExpenseHandler : IRequestHandler<GetExpenseQuery, Expense?>
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<GetExpenseHandler> _logger;

		public GetExpenseHandler(IExpenseRepository repository, ILogger<GetExpenseHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<Expense?> Handle(GetExpenseQuery request, CancellationToken cancellationToken)
		{
			try
			{
				return await _repository.Get(request.Id, request.UserId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch expense {ExpenseId} for user {UserId}", request.Id, request.UserId);
				return null;
			}
		}
	}
}
