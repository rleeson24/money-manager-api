using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Mappers;
using MoneyManager.Data.Repositories;
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
		private readonly IExpenseMapper _mapper;

		public CreateExpenseUseCase(IExpenseRepository repository, ILogger<CreateExpenseUseCase> logger, IExpenseMapper mapper)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public async Task<Expense?> Execute(Guid userId, CreateExpenseModel model)
		{
			try
			{
				var expenseToSave = _mapper.Create(model, userId);
				var id = await _repository.Save(userId, expenseToSave);
				if (id > 0)
				{
					var savedExpense = await _repository.Get(id, userId);
					return savedExpense != null ? _mapper.DbToOutput(savedExpense) : null;
				}
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred creating expense");
				return null;
			}
		}
	}
}
