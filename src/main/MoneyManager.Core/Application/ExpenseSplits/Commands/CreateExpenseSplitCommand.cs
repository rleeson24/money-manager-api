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
		private readonly IExpenseSplitRepository _repository;
		private readonly ILogger<CreateExpenseSplitHandler> _logger;

		public CreateExpenseSplitHandler(IExpenseSplitRepository repository, ILogger<CreateExpenseSplitHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<ExpenseSplit?> Handle(CreateExpenseSplitCommand request, CancellationToken cancellationToken)
		{
			try
			{
				var split = await _repository.Create(request.UserId, request.Model);
				if (split != null)
				{
					_logger.LogInformation(
						"Created expense split {SplitId} for expense {ExpenseId}, user {UserId}",
						split.Id, request.Model.Expense_I, request.UserId);
				}
				return split;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create expense split for expense {ExpenseId}, user {UserId}", request.Model.Expense_I, request.UserId);
				return null;
			}
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
