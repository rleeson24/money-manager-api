using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IBulkDeleteExpensesUseCase
	{
		Task<bool> Execute(IEnumerable<int> ids, Guid userId);
	}

	public class BulkDeleteExpensesUseCase : IBulkDeleteExpensesUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<BulkDeleteExpensesUseCase> _logger;

		public BulkDeleteExpensesUseCase(IExpenseRepository repository, ILogger<BulkDeleteExpensesUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<bool> Execute(IEnumerable<int> ids, Guid userId)
		{
			try
			{
				return await _repository.BulkDelete(ids, userId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred bulk deleting expenses");
				return false;
			}
		}
	}
}
