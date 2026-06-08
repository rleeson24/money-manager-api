using FluentValidation;
using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.ExpenseSplits.Commands
{
	public record UpdateExpenseSplitCommand(int Id, Guid UserId, CreateOrUpdateExpenseSplitModel Model) : IRequest<ExpenseSplit?>;

	public class UpdateExpenseSplitHandler : IRequestHandler<UpdateExpenseSplitCommand, ExpenseSplit?>
	{
		private readonly IExpenseRepository _expenseRepository;
		private readonly IExpenseSplitRepository _splitRepository;
		private readonly ILogger<UpdateExpenseSplitHandler> _logger;

		public UpdateExpenseSplitHandler(
			IExpenseRepository expenseRepository,
			IExpenseSplitRepository splitRepository,
			ILogger<UpdateExpenseSplitHandler> logger)
		{
			_expenseRepository = expenseRepository;
			_splitRepository = splitRepository;
			_logger = logger;
		}

		public async Task<ExpenseSplit?> Handle(UpdateExpenseSplitCommand request, CancellationToken cancellationToken)
		{
			var expense = await _expenseRepository.Get(request.Model.Expense_I, request.UserId);
			if (expense == null)
			{
				_logger.LogWarning("Update split failed: expense {ExpenseId} not found for user {UserId}", request.Model.Expense_I, request.UserId);
				return null;
			}

			var split = await _splitRepository.Update(request.Id, request.UserId, request.Model);
			if (split != null)
				_logger.LogInformation("Updated expense split {SplitId} for user {UserId}", request.Id, request.UserId);
			return split;
		}
	}

	public class UpdateExpenseSplitCommandValidator : AbstractValidator<UpdateExpenseSplitCommand>
	{
		public UpdateExpenseSplitCommandValidator()
		{
			RuleFor(x => x.Id).GreaterThan(0);
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Model.Expense_I).GreaterThan(0);
		}
	}
}
