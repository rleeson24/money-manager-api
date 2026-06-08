using FluentValidation;
using MediatR;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Commands
{
	public record BulkDeleteExpensesCommand(IEnumerable<int> Ids, Guid UserId) : IRequest<bool>;

	public class BulkDeleteExpensesHandler : IRequestHandler<BulkDeleteExpensesCommand, bool>
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<BulkDeleteExpensesHandler> _logger;

		public BulkDeleteExpensesHandler(IExpenseRepository repository, ILogger<BulkDeleteExpensesHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<bool> Handle(BulkDeleteExpensesCommand request, CancellationToken cancellationToken)
		{
			var idList = request.Ids.ToList();
			var success = await _repository.BulkDelete(idList, request.UserId);
			if (success)
				_logger.LogInformation("Bulk deleted {Count} expenses for user {UserId}", idList.Count, request.UserId);
			return success;
		}
	}

	public class BulkDeleteExpensesCommandValidator : AbstractValidator<BulkDeleteExpensesCommand>
	{
		public BulkDeleteExpensesCommandValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Ids).NotEmpty();
		}
	}
}
