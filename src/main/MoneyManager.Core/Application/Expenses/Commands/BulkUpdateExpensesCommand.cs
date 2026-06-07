using FluentValidation;
using MediatR;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Commands
{
	public record BulkUpdateExpensesCommand(
		IEnumerable<int> Ids,
		Guid UserId,
		Dictionary<string, object?> Updates) : IRequest<bool>;

	public class BulkUpdateExpensesHandler : IRequestHandler<BulkUpdateExpensesCommand, bool>
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<BulkUpdateExpensesHandler> _logger;

		public BulkUpdateExpensesHandler(IExpenseRepository repository, ILogger<BulkUpdateExpensesHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<bool> Handle(BulkUpdateExpensesCommand request, CancellationToken cancellationToken)
		{
			var idList = request.Ids.ToList();
			try
			{
				var success = await _repository.BulkUpdate(idList, request.UserId, request.Updates);
				if (success)
				{
					_logger.LogInformation(
						"Bulk updated {Count} expenses for user {UserId} ({FieldCount} fields)",
						idList.Count, request.UserId, request.Updates.Count);
				}
				return success;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to bulk update {Count} expenses for user {UserId}", idList.Count, request.UserId);
				return false;
			}
		}
	}

	public class BulkUpdateExpensesCommandValidator : AbstractValidator<BulkUpdateExpensesCommand>
	{
		public BulkUpdateExpensesCommandValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Ids).NotEmpty();
		}
	}
}
