using FluentValidation;
using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Commands
{
	public record UpdateExpenseCommand(int Id, Guid UserId, Expense Expense) : IRequest<UpdateExpenseResult>;

	public class UpdateExpenseHandler : IRequestHandler<UpdateExpenseCommand, UpdateExpenseResult>
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<UpdateExpenseHandler> _logger;

		public UpdateExpenseHandler(IExpenseRepository repository, ILogger<UpdateExpenseHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<UpdateExpenseResult> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
		{
			var result = await _repository.Update(request.Id, request.UserId, request.Expense);
			if (result.IsSuccess)
				_logger.LogInformation("Updated expense {ExpenseId} for user {UserId}", request.Id, request.UserId);
			else if (result.IsConflict)
				_logger.LogWarning("Update conflict on expense {ExpenseId} for user {UserId}", request.Id, request.UserId);
			return result;
		}
	}

	public class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
	{
		public UpdateExpenseCommandValidator()
		{
			RuleFor(x => x.Id).GreaterThan(0);
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Expense.ExpenseDescription).NotEmpty().MaximumLength(500);
			RuleFor(x => x.Expense.Currency).NotEmpty().MaximumLength(10);
		}
	}
}
