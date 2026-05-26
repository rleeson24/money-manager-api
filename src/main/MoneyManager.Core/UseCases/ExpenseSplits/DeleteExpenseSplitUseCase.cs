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
				var deleted = await _repository.Delete(id, userId);
				if (deleted)
				{
					_logger.LogInformation("Deleted expense split {SplitId} for user {UserId}", id, userId);
				}
				return deleted;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete expense split {SplitId} for user {UserId}", id, userId);
				return false;
			}
		}
	}
}
