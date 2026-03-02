using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.ExpenseSplits
{
	public interface IGetExpenseSplitsUseCase
	{
		Task<IReadOnlyList<ExpenseSplit>> Execute(int expense_I, Guid userId);
	}

	public class GetExpenseSplitsUseCase : IGetExpenseSplitsUseCase
	{
		private readonly IExpenseSplitRepository _repository;
		private readonly ILogger<GetExpenseSplitsUseCase> _logger;

		public GetExpenseSplitsUseCase(IExpenseSplitRepository repository, ILogger<GetExpenseSplitsUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<ExpenseSplit>> Execute(int expense_I, Guid userId)
		{
			try
			{
				return await _repository.GetByExpenseId(expense_I, userId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching expense splits for {ExpenseId}", expense_I);
				return Array.Empty<ExpenseSplit>();
			}
		}
	}
}
