using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IUpdateExpenseUseCase
	{
		Task<Expense?> Execute(int id, Guid userId, CreateExpenseModel model);
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

		public async Task<Expense?> Execute(int id, Guid userId, CreateExpenseModel model)
		{
			try
			{
				return await _repository.Update(id, userId, model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred updating expense {ExpenseId}", id);
				return null;
			}
		}
	}
}
