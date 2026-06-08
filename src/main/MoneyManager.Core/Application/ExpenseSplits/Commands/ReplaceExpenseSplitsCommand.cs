using FluentValidation;
using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.ExpenseSplits.Commands
{
	public record ReplaceExpenseSplitsCommand(int ExpenseId, Guid UserId, ReplaceExpenseSplitsRequest Request) : IRequest<ReplaceSplitsResult>;

	public class ReplaceExpenseSplitsHandler : IRequestHandler<ReplaceExpenseSplitsCommand, ReplaceSplitsResult>
	{
		private readonly IExpenseRepository _expenseRepository;
		private readonly IExpenseSplitRepository _splitRepository;
		private readonly ILogger<ReplaceExpenseSplitsHandler> _logger;

		public ReplaceExpenseSplitsHandler(
			IExpenseRepository expenseRepository,
			IExpenseSplitRepository splitRepository,
			ILogger<ReplaceExpenseSplitsHandler> logger)
		{
			_expenseRepository = expenseRepository;
			_splitRepository = splitRepository;
			_logger = logger;
		}

		public async Task<ReplaceSplitsResult> Handle(ReplaceExpenseSplitsCommand request, CancellationToken cancellationToken)
		{
			var expense = await _expenseRepository.Get(request.ExpenseId, request.UserId);
			if (expense == null)
			{
				_logger.LogWarning("Replace splits failed: expense {ExpenseId} not found for user {UserId}", request.ExpenseId, request.UserId);
				return ReplaceSplitsResult.Failure("Expense not found.");
			}

			var parentAmount = expense.Amount;
			var items = request.Request.Splits ?? new List<ReplaceExpenseSplitItemModel>();
			var result = await _splitRepository.ReplaceByExpenseId(request.ExpenseId, request.UserId, parentAmount, items);
			if (result.IsSuccess)
			{
				_logger.LogInformation(
					"Replaced {SplitCount} splits for expense {ExpenseId}, user {UserId}",
					items.Count, request.ExpenseId, request.UserId);
			}
			else
			{
				_logger.LogWarning(
					"Replace splits validation failed for expense {ExpenseId}, user {UserId}: {Error}",
					request.ExpenseId, request.UserId, result.ValidationError);
			}
			return result;
		}
	}

	public class ReplaceExpenseSplitsCommandValidator : AbstractValidator<ReplaceExpenseSplitsCommand>
	{
		public ReplaceExpenseSplitsCommandValidator()
		{
			RuleFor(x => x.ExpenseId).GreaterThan(0);
			RuleFor(x => x.UserId).NotEmpty();
		}
	}
}
