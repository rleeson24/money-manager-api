using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Mappers;
using MoneyManager.Data.Repositories;
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
		private readonly IExpenseMapper _mapper;

		public UpdateExpenseUseCase(IExpenseRepository repository, ILogger<UpdateExpenseUseCase> logger, IExpenseMapper mapper)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public async Task<Expense?> Execute(int id, Guid userId, CreateExpenseModel model)
		{
			try
			{
				var existing = await _repository.Get(id, userId);
				if (existing == null)
					return null;

				var updated = _mapper.Update(existing, model);
				await _repository.Save(userId, updated);
				
				var savedExpense = await _repository.Get(id, userId);
				return savedExpense != null ? _mapper.DbToOutput(savedExpense) : null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred updating expense {ExpenseId}", id);
				return null;
			}
		}
	}
}
