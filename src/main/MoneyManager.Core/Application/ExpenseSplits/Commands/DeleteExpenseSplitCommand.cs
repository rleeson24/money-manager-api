using FluentValidation;
using MediatR;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.ExpenseSplits.Commands
{
	public record DeleteExpenseSplitCommand(int Id, Guid UserId) : IRequest<bool>;

	public class DeleteExpenseSplitHandler : IRequestHandler<DeleteExpenseSplitCommand, bool>
	{
		private readonly IExpenseSplitRepository _repository;
		private readonly ILogger<DeleteExpenseSplitHandler> _logger;

		public DeleteExpenseSplitHandler(IExpenseSplitRepository repository, ILogger<DeleteExpenseSplitHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<bool> Handle(DeleteExpenseSplitCommand request, CancellationToken cancellationToken)
		{
			try
			{
				var deleted = await _repository.Delete(request.Id, request.UserId);
				if (deleted)
					_logger.LogInformation("Deleted expense split {SplitId} for user {UserId}", request.Id, request.UserId);
				return deleted;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete expense split {SplitId} for user {UserId}", request.Id, request.UserId);
				return false;
			}
		}
	}

	public class DeleteExpenseSplitCommandValidator : AbstractValidator<DeleteExpenseSplitCommand>
	{
		public DeleteExpenseSplitCommandValidator()
		{
			RuleFor(x => x.Id).GreaterThan(0);
			RuleFor(x => x.UserId).NotEmpty();
		}
	}
}
