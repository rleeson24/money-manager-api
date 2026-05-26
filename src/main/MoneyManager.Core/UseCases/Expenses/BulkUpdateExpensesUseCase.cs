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
			var idList = ids.ToList();
			try
			{
				var success = await _repository.BulkUpdate(idList, userId, updates);
				if (success)
				{
					_logger.LogInformation(
						"Bulk updated {Count} expenses for user {UserId} ({FieldCount} fields)",
						idList.Count, userId, updates.Count);
				}
				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to bulk update {Count} expenses for user {UserId}", idList.Count, userId);
				return false;
			}
		}
	}
}
