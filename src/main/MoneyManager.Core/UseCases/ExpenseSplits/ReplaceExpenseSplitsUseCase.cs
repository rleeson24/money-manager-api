using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.ExpenseSplits
{
	public interface IReplaceExpenseSplitsUseCase
	{
		Task<ReplaceSplitsResult> Execute(int expenseId, Guid userId, ReplaceExpenseSplitsRequest request);
	}

	public class ReplaceExpenseSplitsUseCase : IReplaceExpenseSplitsUseCase
	{
		private readonly IExpenseRepository _expenseRepository;
		private readonly IExpenseSplitRepository _splitRepository;
		private readonly ILogger<ReplaceExpenseSplitsUseCase> _logger;

		public ReplaceExpenseSplitsUseCase(
			IExpenseRepository expenseRepository,
			IExpenseSplitRepository splitRepository,
			ILogger<ReplaceExpenseSplitsUseCase> logger)
		{
			_expenseRepository = expenseRepository;
			_splitRepository = splitRepository;
			_logger = logger;
		}

		public async Task<ReplaceSplitsResult> Execute(int expenseId, Guid userId, ReplaceExpenseSplitsRequest request)
		{
			try
			{
				var expense = await _expenseRepository.Get(expenseId, userId);
				if (expense == null)
				{
					_logger.LogWarning("Replace splits failed: expense {ExpenseId} not found for user {UserId}", expenseId, userId);
					return ReplaceSplitsResult.Failure("Expense not found.");
				}

				var parentAmount = expense.Amount;
				var items = request.Splits ?? new List<ReplaceExpenseSplitItemModel>();
				var result = await _splitRepository.ReplaceByExpenseId(expenseId, userId, parentAmount, items);
				if (result.IsSuccess)
				{
					_logger.LogInformation(
						"Replaced {SplitCount} splits for expense {ExpenseId}, user {UserId}",
						items.Count, expenseId, userId);
				}
				else
				{
					_logger.LogWarning(
						"Replace splits validation failed for expense {ExpenseId}, user {UserId}: {Error}",
						expenseId, userId, result.ValidationError);
				}
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to replace splits for expense {ExpenseId}, user {UserId}", expenseId, userId);
				return ReplaceSplitsResult.Failure("An unexpected error occurred.");
			}
		}
	}
}
