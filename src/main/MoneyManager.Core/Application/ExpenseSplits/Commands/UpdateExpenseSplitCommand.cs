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
		private readonly IExpenseSplitRepository _repository;
		private readonly ILogger<UpdateExpenseSplitHandler> _logger;

		public UpdateExpenseSplitHandler(IExpenseSplitRepository repository, ILogger<UpdateExpenseSplitHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<ExpenseSplit?> Handle(UpdateExpenseSplitCommand request, CancellationToken cancellationToken)
		{
			try
			{
				var split = await _repository.Update(request.Id, request.UserId, request.Model);
				if (split != null)
					_logger.LogInformation("Updated expense split {SplitId} for user {UserId}", request.Id, request.UserId);
				return split;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update expense split {SplitId} for user {UserId}", request.Id, request.UserId);
				return null;
			}
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
