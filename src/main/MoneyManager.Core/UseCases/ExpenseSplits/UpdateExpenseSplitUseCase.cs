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
				var split = await _repository.Update(id, userId, model);
				if (split != null)
				{
					_logger.LogInformation("Updated expense split {SplitId} for user {UserId}", id, userId);
				}
				return split;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update expense split {SplitId} for user {UserId}", id, userId);
				return null;
			}
		}
	}
}
