using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IPatchExpenseUseCase
	{
		Task<Expense?> Execute(int id, Guid userId, Dictionary<string, object?> updates);
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

		public async Task<Expense?> Execute(int id, Guid userId, Dictionary<string, object?> updates)
		{
			try
			{
				return await _repository.Patch(id, userId, updates);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred patching expense {ExpenseId}", id);
				return null;
			}
		}
	}
}
