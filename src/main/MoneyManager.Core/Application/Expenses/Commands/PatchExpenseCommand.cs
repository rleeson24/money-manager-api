using FluentValidation;
using MediatR;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MoneyManager.Core.Application.Expenses.Commands
{
	public record PatchExpenseCommand(
		int Id,
		Guid UserId,
		JsonElement PatchBody) : IRequest<UpdateExpenseResult>;

	public class PatchExpenseHandler : IRequestHandler<PatchExpenseCommand, UpdateExpenseResult>
	{
		private readonly IExpenseRepository _repository;
		private readonly IExpensePatchParser _patchParser;
		private readonly ILogger<PatchExpenseHandler> _logger;

		public PatchExpenseHandler(
			IExpenseRepository repository,
			IExpensePatchParser patchParser,
			ILogger<PatchExpenseHandler> logger)
		{
			_repository = repository;
			_patchParser = patchParser;
			_logger = logger;
		}

		public async Task<UpdateExpenseResult> Handle(PatchExpenseCommand request, CancellationToken cancellationToken)
		{
			var parsed = _patchParser.Parse(request.PatchBody);
			var result = await _repository.Patch(
				request.Id, request.UserId, parsed.Updates, parsed.ExpectedModifiedDateTime);
			if (result.IsSuccess)
			{
				_logger.LogInformation(
					"Patched expense {ExpenseId} for user {UserId} ({FieldCount} fields)",
					request.Id, request.UserId, parsed.Updates.Count);
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
		public PatchExpenseCommandValidator(IExpensePatchParser patchParser)
		{
			RuleFor(x => x.Id).GreaterThan(0);
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.PatchBody).Custom((patchBody, context) =>
			{
				var parsed = patchParser.Parse(patchBody);
				if (parsed.Updates.Keys.Any(key => !ExpenseFieldNames.IsPatchableField(key)))
					context.AddFailure("One or more patch fields are not supported.");
			});
		}
	}
}
