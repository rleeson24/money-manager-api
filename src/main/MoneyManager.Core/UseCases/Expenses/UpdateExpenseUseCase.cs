using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IUpdateExpenseUseCase
	{
		Task<UpdateExpenseResult> Execute(int id, Guid userId, Expense expense);
	}

	public class UpdateExpenseUseCase : IUpdateExpenseUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<UpdateExpenseUseCase> _logger;

		public UpdateExpenseUseCase(IExpenseRepository repository, ILogger<UpdateExpenseUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<UpdateExpenseResult> Execute(int id, Guid userId, Expense expense)
		{
			try
			{
				var result = await _repository.Update(id, userId, expense);
				if (result.IsSuccess)
				{
					_logger.LogInformation("Updated expense {ExpenseId} for user {UserId}", id, userId);
				}
				else if (result.IsConflict)
				{
					_logger.LogWarning("Update conflict on expense {ExpenseId} for user {UserId}", id, userId);
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update expense {ExpenseId} for user {UserId}", id, userId);
				return UpdateExpenseResult.NotFound();
			}
		}
	}
}
