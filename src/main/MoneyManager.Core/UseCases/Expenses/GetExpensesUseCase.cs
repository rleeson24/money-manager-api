using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Expenses
{
	public interface IGetExpensesUseCase
	{
		Task<IReadOnlyList<Expense>?> Execute(Guid userId, string? month = null);
		Task<IReadOnlyList<Expense>?> ExecuteWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null);
	}

	public class GetExpensesUseCase : IGetExpensesUseCase
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<GetExpensesUseCase> _logger;

		public GetExpensesUseCase(IExpenseRepository repository, ILogger<GetExpensesUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<Expense>?> Execute(Guid userId, string? month = null)
		{
			try
			{
				return await _repository.ListForUser(userId, month);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching expenses");
				return null;
			}
		}

		public async Task<IReadOnlyList<Expense>?> ExecuteWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null)
		{
			try
			{
				return await _repository.ListForUserWithFilters(userId, paymentMethod, datePaidNull);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching expenses with filters");
				return null;
			}
		}
	}
}
