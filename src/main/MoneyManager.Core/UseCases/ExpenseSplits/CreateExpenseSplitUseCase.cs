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
				return await _repository.Create(userId, model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred creating expense split");
				return null;
			}
		}
	}
}
