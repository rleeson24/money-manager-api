using MoneyManager.Core.Models;
using MoneyManager.Core.Mappers;
using MoneyManager.Data.Repositories;
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
		private readonly IExpenseMapper _mapper;

		public GetExpensesUseCase(IExpenseRepository repository, ILogger<GetExpensesUseCase> logger, IExpenseMapper mapper)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public async Task<IEnumerable<Expense>?> Execute(Guid userId, string? month = null)
		{
			try
			{
				var expenses = await _repository.ListForUser(userId, month);
				return expenses.Select(e => _mapper.DbToOutput(e));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching expenses");
				return null;
			}
		}
	}
}
