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
				var expenses = await _repository.ListForUser(userId, month);
				_logger.LogDebug(
					"Fetched {Count} expenses for user {UserId} (month={Month})",
					expenses.Count, userId, month ?? "all");
				return expenses;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch expenses for user {UserId}", userId);
				return null;
			}
		}

		public async Task<IReadOnlyList<Expense>?> ExecuteWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null)
		{
			try
			{
				var expenses = await _repository.ListForUserWithFilters(userId, paymentMethod, datePaidNull);
				_logger.LogDebug(
					"Fetched {Count} filtered expenses for user {UserId} (paymentMethod={PaymentMethod}, datePaidNull={DatePaidNull})",
					expenses.Count, userId, paymentMethod, datePaidNull);
				return expenses;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch filtered expenses for user {UserId}", userId);
				return null;
			}
		}
	}
}
