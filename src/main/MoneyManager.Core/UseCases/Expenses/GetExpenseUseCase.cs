using MoneyManager.Core.Models;
using MoneyManager.Core.Mappers;
using MoneyManager.Data.Repositories;
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
		private readonly IExpenseMapper _mapper;

		public GetExpenseUseCase(IExpenseRepository repository, ILogger<GetExpenseUseCase> logger, IExpenseMapper mapper)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public async Task<Expense?> Execute(int id, Guid userId)
		{
			try
			{
				var expense = await _repository.Get(id, userId);
				if (expense == null)
					return null;

				return _mapper.DbToOutput(expense);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching expense {ExpenseId}", id);
				return null;
			}
		}
	}
}
