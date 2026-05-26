using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IDeleteExpenseUseCase
	{
		Task<bool> Execute(int id, Guid userId);
	}

	public class DeleteExpenseUseCase : IDeleteExpenseUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<DeleteExpenseUseCase> _logger;

		public DeleteExpenseUseCase(IExpenseRepository repository, ILogger<DeleteExpenseUseCase> logger)
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
					_logger.LogInformation("Deleted expense {ExpenseId} for user {UserId}", id, userId);
				}
				return deleted;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete expense {ExpenseId} for user {UserId}", id, userId);
				return false;
			}
		}
	}
}
