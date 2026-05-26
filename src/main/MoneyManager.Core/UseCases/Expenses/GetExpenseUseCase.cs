using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IGetExpenseUseCase
	{
		Task<Expense?> Execute(int id, Guid userId);
	}

	public class GetExpenseUseCase : IGetExpenseUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<GetExpenseUseCase> _logger;

		public GetExpenseUseCase(IExpenseRepository repository, ILogger<GetExpenseUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<Expense?> Execute(int id, Guid userId)
		{
			try
			{
				return await _repository.Get(id, userId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch expense {ExpenseId} for user {UserId}", id, userId);
				return null;
			}
		}
	}
}
