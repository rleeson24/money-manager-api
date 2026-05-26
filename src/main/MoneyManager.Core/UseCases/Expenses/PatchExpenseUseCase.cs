using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IPatchExpenseUseCase
	{
		Task<UpdateExpenseResult> Execute(int id, Guid userId, Dictionary<string, object?> updates, DateTime? expectedModifiedDateTime);
	}

	public class PatchExpenseUseCase : IPatchExpenseUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<PatchExpenseUseCase> _logger;

		public PatchExpenseUseCase(IExpenseRepository repository, ILogger<PatchExpenseUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<UpdateExpenseResult> Execute(int id, Guid userId, Dictionary<string, object?> updates, DateTime? expectedModifiedDateTime)
		{
			try
			{
				var result = await _repository.Patch(id, userId, updates, expectedModifiedDateTime);
				if (result.IsSuccess)
				{
					_logger.LogInformation(
						"Patched expense {ExpenseId} for user {UserId} ({FieldCount} fields)",
						id, userId, updates.Count);
				}
				else if (result.IsConflict)
				{
					_logger.LogWarning("Patch conflict on expense {ExpenseId} for user {UserId}", id, userId);
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to patch expense {ExpenseId} for user {UserId}", id, userId);
				return UpdateExpenseResult.NotFound();
			}
		}
	}
}
