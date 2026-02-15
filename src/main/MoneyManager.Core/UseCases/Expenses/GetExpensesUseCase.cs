using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IGetExpensesUseCase
	{
		Task<IEnumerable<Expense>?> Execute(Guid userId, string? month = null);
	}

	public class GetExpensesUseCase : IGetExpensesUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<GetExpensesUseCase> _logger;

		public GetExpensesUseCase(IExpenseRepository repository, ILogger<GetExpensesUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IEnumerable<Expense>?> Execute(Guid userId, string? month = null)
		{
			try
			{
				return await _repository.ListForUser(userId, month);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching expenses");
				return null;
			}
		}
	}
}
