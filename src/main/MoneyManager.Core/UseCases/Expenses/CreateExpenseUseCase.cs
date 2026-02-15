using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface ICreateExpenseUseCase
	{
		Task<Expense?> Execute(Guid userId, CreateExpenseModel model);
	}

	public class CreateExpenseUseCase : ICreateExpenseUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<CreateExpenseUseCase> _logger;

		public CreateExpenseUseCase(IExpenseRepository repository, ILogger<CreateExpenseUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<Expense?> Execute(Guid userId, CreateExpenseModel model)
		{
			try
			{
				return await _repository.Create(userId, model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred creating expense");
				return null;
			}
		}
	}
}
