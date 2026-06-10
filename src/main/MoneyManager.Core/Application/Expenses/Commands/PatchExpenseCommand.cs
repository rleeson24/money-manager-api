using FluentValidation;
using MediatR;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Commands
{
	public record PatchExpenseCommand(
		int Id,
		Guid UserId,
		Dictionary<string, object?> Updates,
		DateTime? ExpectedModifiedDateTime) : IRequest<UpdateExpenseResult>;

	public class PatchExpenseHandler : IRequestHandler<PatchExpenseCommand, UpdateExpenseResult>
	{
		private readonly IExpenseRepository _repository;
		private readonly ILogger<PatchExpenseHandler> _logger;

		public PatchExpenseHandler(IExpenseRepository repository, ILogger<PatchExpenseHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<UpdateExpenseResult> Handle(PatchExpenseCommand request, CancellationToken cancellationToken)
		{
			var result = await _repository.Patch(
				request.Id, request.UserId, request.Updates, request.ExpectedModifiedDateTime);
			if (result.IsSuccess)
			{
				_logger.LogInformation(
					"Patched expense {ExpenseId} for user {UserId} ({FieldCount} fields)",
					request.Id, request.UserId, request.Updates.Count);
			}
			else if (result.IsConflict)
			{
				_logger.LogWarning("Patch conflict on expense {ExpenseId} for user {UserId}", request.Id, request.UserId);
			}
			return result;
		}
	}

	public class PatchExpenseCommandValidator : AbstractValidator<PatchExpenseCommand>
	{
		public PatchExpenseCommandValidator()
		{
			RuleFor(x => x.Id).GreaterThan(0);
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Updates)
				.Must(updates => updates.Keys.All(ExpenseFieldNames.IsPatchableField))
				.WithMessage("One or more patch fields are not supported.");
		}
	}
}
