using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IBulkUpdateExpensesUseCase
	{
		Task<bool> Execute(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates);
	}

	public class BulkUpdateExpensesUseCase : IBulkUpdateExpensesUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<BulkUpdateExpensesUseCase> _logger;

		public BulkUpdateExpensesUseCase(IExpenseRepository repository, ILogger<BulkUpdateExpensesUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<bool> Execute(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates)
		{
			try
			{
				return await _repository.BulkUpdate(ids, userId, updates);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred bulk updating expenses");
				return false;
			}
		}
	}
}
