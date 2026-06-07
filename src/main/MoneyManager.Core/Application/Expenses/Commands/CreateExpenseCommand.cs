using FluentValidation;
using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Commands
{
	public record CreateExpenseCommand(Guid UserId, CreateExpenseModel Model) : IRequest<Expense?>;

	public class CreateExpenseHandler : IRequestHandler<CreateExpenseCommand, Expense?>
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<CreateExpenseHandler> _logger;

		public CreateExpenseHandler(IExpenseRepository repository, ILogger<CreateExpenseHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<Expense?> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
		{
			try
			{
				request.Model.CreatedBy ??= request.UserId.ToString();
				var expense = await _repository.Create(request.UserId, request.Model);
				if (expense != null)
				{
					_logger.LogInformation(
						"Created expense {ExpenseId} for user {UserId}: {Amount} on {ExpenseDate}",
						expense.Expense_I, request.UserId, expense.Amount, expense.ExpenseDate);
				}
				return expense;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create expense for user {UserId}", request.UserId);
				return null;
			}
		}
	}

	public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
	{
		public CreateExpenseCommandValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Model.Expense).NotEmpty().MaximumLength(500);
			RuleFor(x => x.Model.Currency).NotEmpty().MaximumLength(10);
		}
	}
}
