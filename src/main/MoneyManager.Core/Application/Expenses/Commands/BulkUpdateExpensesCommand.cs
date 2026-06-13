using FluentValidation;
using MediatR;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Commands
{
	public record BulkUpdateExpensesCommand(Guid UserId, BulkUpdateRequest Request) : IRequest<bool>;

	public class BulkUpdateExpensesHandler : IRequestHandler<BulkUpdateExpensesCommand, bool>
	{
		private readonly IExpenseRepository _repository;
		private readonly IExpenseBulkUpdateMapper _bulkUpdateMapper;
		private readonly ILogger<BulkUpdateExpensesHandler> _logger;

		public BulkUpdateExpensesHandler(
			IExpenseRepository repository,
			IExpenseBulkUpdateMapper bulkUpdateMapper,
			ILogger<BulkUpdateExpensesHandler> logger)
		{
			_repository = repository;
			_bulkUpdateMapper = bulkUpdateMapper;
			_logger = logger;
		}

		public async Task<bool> Handle(BulkUpdateExpensesCommand request, CancellationToken cancellationToken)
		{
			var updates = _bulkUpdateMapper.ToUpdates(request.Request);
			var idList = request.Request.Ids;
			var success = await _repository.BulkUpdate(idList, request.UserId, updates);
			if (success)
			{
				_logger.LogInformation(
					"Bulk updated {Count} expenses for user {UserId} ({FieldCount} fields)",
					idList.Count, request.UserId, updates.Count);
			}
			return success;
		}
	}

	public class BulkUpdateExpensesCommandValidator : AbstractValidator<BulkUpdateExpensesCommand>
	{
		public BulkUpdateExpensesCommandValidator(IExpenseBulkUpdateMapper bulkUpdateMapper)
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Request.Ids).NotEmpty();
			RuleFor(x => x.Request).Custom((bulkRequest, context) =>
			{
				var updates = bulkUpdateMapper.ToUpdates(bulkRequest);
				if (updates.Count == 0)
					context.AddFailure("At least one bulk update field is required.");
				if (updates.Keys.Any(key => !ExpenseFieldNames.IsBulkUpdateField(key)))
					context.AddFailure("One or more bulk update fields are not supported.");
			});
		}
	}
}
