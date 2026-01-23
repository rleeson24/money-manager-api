using MoneyManager.Data.Repositories;
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
				return await _repository.Delete(id, userId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred deleting expense {ExpenseId}", id);
				return false;
			}
		}
	}
}
