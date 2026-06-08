using FluentValidation;
using MediatR;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Commands
{
	public record DeleteExpenseCommand(int Id, Guid UserId) : IRequest<bool>;

	public class DeleteExpenseHandler : IRequestHandler<DeleteExpenseCommand, bool>
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<DeleteExpenseHandler> _logger;

		public DeleteExpenseHandler(IExpenseRepository repository, ILogger<DeleteExpenseHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<bool> Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
		{
			var deleted = await _repository.Delete(request.Id, request.UserId);
			if (deleted)
				_logger.LogInformation("Deleted expense {ExpenseId} for user {UserId}", request.Id, request.UserId);
			return deleted;
		}
	}

	public class DeleteExpenseCommandValidator : AbstractValidator<DeleteExpenseCommand>
	{
		public DeleteExpenseCommandValidator()
		{
			RuleFor(x => x.Id).GreaterThan(0);
			RuleFor(x => x.UserId).NotEmpty();
		}
	}
}
