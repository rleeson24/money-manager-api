using FluentValidation;
using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.ExpenseSplits.Commands
{
	public record CreateExpenseSplitCommand(Guid UserId, CreateOrUpdateExpenseSplitModel Model) : IRequest<ExpenseSplit?>;

	public class CreateExpenseSplitHandler : IRequestHandler<CreateExpenseSplitCommand, ExpenseSplit?>
	{
		private readonly IExpenseRepository _expenseRepository;
		private readonly IExpenseSplitRepository _splitRepository;
		private readonly ILogger<CreateExpenseSplitHandler> _logger;

		public CreateExpenseSplitHandler(
			IExpenseRepository expenseRepository,
			IExpenseSplitRepository splitRepository,
			ILogger<CreateExpenseSplitHandler> logger)
		{
			_expenseRepository = expenseRepository;
			_splitRepository = splitRepository;
			_logger = logger;
		}

		public async Task<ExpenseSplit?> Handle(CreateExpenseSplitCommand request, CancellationToken cancellationToken)
		{
			var expense = await _expenseRepository.Get(request.Model.Expense_I, request.UserId);
			if (expense == null)
			{
				_logger.LogWarning("Create split failed: expense {ExpenseId} not found for user {UserId}", request.Model.Expense_I, request.UserId);
				return null;
			}

			var split = await _splitRepository.Create(request.UserId, request.Model);
			if (split != null)
			{
				_logger.LogInformation(
					"Created expense split {SplitId} for expense {ExpenseId}, user {UserId}",
					split.Id, request.Model.Expense_I, request.UserId);
			}
			return split;
		}
	}

	public class CreateExpenseSplitCommandValidator : AbstractValidator<CreateExpenseSplitCommand>
	{
		public CreateExpenseSplitCommandValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Model.Expense_I).GreaterThan(0);
		}
	}
}
