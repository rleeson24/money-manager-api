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
			var expense = await _expenseRepository.Get(expenseId, userId);
			if (expense == null)
				return ReplaceSplitsResult.Failure("Expense not found.");
			var parentAmount = expense.Amount;
			var items = request.Splits ?? new List<ReplaceExpenseSplitItemModel>();
			return await _splitRepository.ReplaceByExpenseId(expenseId, userId, parentAmount, items);
		}
	}
}
