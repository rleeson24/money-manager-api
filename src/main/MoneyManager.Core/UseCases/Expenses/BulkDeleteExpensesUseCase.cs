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
			var idList = ids.ToList();
			try
			{
				var success = await _repository.BulkDelete(idList, userId);
				if (success)
				{
					_logger.LogInformation("Bulk deleted {Count} expenses for user {UserId}", idList.Count, userId);
				}
				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to bulk delete {Count} expenses for user {UserId}", idList.Count, userId);
				return false;
			}
		}
	}
}
