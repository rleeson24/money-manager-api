using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.ExpenseSplits
{
	public interface ICreateExpenseSplitUseCase
	{
		Task<ExpenseSplit?> Execute(Guid userId, CreateOrUpdateExpenseSplitModel model);
	}

	public class CreateExpenseSplitUseCase : ICreateExpenseSplitUseCase
	{
		private readonly IExpenseSplitRepository _repository;
		private readonly ILogger<CreateExpenseSplitUseCase> _logger;

		public CreateExpenseSplitUseCase(IExpenseSplitRepository repository, ILogger<CreateExpenseSplitUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<ExpenseSplit?> Execute(Guid userId, CreateOrUpdateExpenseSplitModel model)
		{
			try
			{
				var split = await _repository.Create(userId, model);
				if (split != null)
				{
					_logger.LogInformation(
						"Created expense split {SplitId} for expense {ExpenseId}, user {UserId}",
						split.Id, model.Expense_I, userId);
				}
				return split;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create expense split for expense {ExpenseId}, user {UserId}", model.Expense_I, userId);
				return null;
			}
		}
	}
}
