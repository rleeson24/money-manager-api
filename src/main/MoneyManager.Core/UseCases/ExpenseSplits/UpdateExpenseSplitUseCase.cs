using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.ExpenseSplits
{
	public interface IUpdateExpenseSplitUseCase
	{
		Task<ExpenseSplit?> Execute(int id, Guid userId, CreateOrUpdateExpenseSplitModel model);
	}

	public class UpdateExpenseSplitUseCase : IUpdateExpenseSplitUseCase
	{
		private readonly IExpenseSplitRepository _repository;
		private readonly ILogger<UpdateExpenseSplitUseCase> _logger;

		public UpdateExpenseSplitUseCase(IExpenseSplitRepository repository, ILogger<UpdateExpenseSplitUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<ExpenseSplit?> Execute(int id, Guid userId, CreateOrUpdateExpenseSplitModel model)
		{
			try
			{
				return await _repository.Update(id, userId, model);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred updating expense split {SplitId}", id);
				return null;
			}
		}
	}
}
