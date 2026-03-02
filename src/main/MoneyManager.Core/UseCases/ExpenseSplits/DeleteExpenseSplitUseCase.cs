using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.ExpenseSplits
{
	public interface IDeleteExpenseSplitUseCase
	{
		Task<bool> Execute(int id, Guid userId);
	}

	public class DeleteExpenseSplitUseCase : IDeleteExpenseSplitUseCase
	{
		private readonly IExpenseSplitRepository _repository;
		private readonly ILogger<DeleteExpenseSplitUseCase> _logger;

		public DeleteExpenseSplitUseCase(IExpenseSplitRepository repository, ILogger<DeleteExpenseSplitUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<bool> Execute(int id, Guid userId)
		{
			try
			{
				return await _repository.Delete(id, userId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred deleting expense split {SplitId}", id);
				return false;
			}
		}
	}
}
