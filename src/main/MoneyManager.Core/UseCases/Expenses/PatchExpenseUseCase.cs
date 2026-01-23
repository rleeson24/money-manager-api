using MoneyManager.Core.Models;
using MoneyManager.Core.Mappers;
using MoneyManager.Data.Repositories;
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
		private readonly IExpenseMapper _mapper;

		public PatchExpenseUseCase(IExpenseRepository repository, ILogger<PatchExpenseUseCase> logger, IExpenseMapper mapper)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public async Task<Expense?> Execute(int id, Guid userId, Dictionary<string, object?> updates)
		{
			try
			{
				var success = await _repository.Update(id, userId, updates);
				if (!success)
					return null;

				var expense = await _repository.Get(id, userId);
				return expense != null ? _mapper.DbToOutput(expense) : null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred patching expense {ExpenseId}", id);
				return null;
			}
		}
	}
}
