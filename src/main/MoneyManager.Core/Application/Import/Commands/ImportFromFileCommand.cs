using System.Linq;
using FluentValidation;
using MediatR;
using MoneyManager.Core.Constants;
using MoneyManager.Core.Import;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Import.Commands
{
	public record ImportFromFileCommand(
		Guid UserId,
		Stream FileContent,
		string Format,
		ImportSource ImportSource,
		int PaymentMethodId) : IRequest<ImportResult>;

	public class ImportFromFileHandler : IRequestHandler<ImportFromFileCommand, ImportResult>
	{
		private readonly ITransactionFileParser _parser;
		private readonly IExpenseRepository _expenseRepository;
		private readonly ILogger<ImportFromFileHandler> _logger;

		public ImportFromFileHandler(
			ITransactionFileParser parser,
			IExpenseRepository expenseRepository,
			ILogger<ImportFromFileHandler> logger)
		{
			_parser = parser;
			_expenseRepository = expenseRepository;
			_logger = logger;
		}

		public async Task<ImportResult> Handle(ImportFromFileCommand request, CancellationToken cancellationToken)
		{
			_logger.LogInformation(
				"Starting import for user {UserId}: format={Format}, source={ImportSource}, paymentMethod={PaymentMethodId}",
				request.UserId, request.Format, request.ImportSource, request.PaymentMethodId);

			var errors = new List<string>();
			IReadOnlyList<BankTransaction> transactions;
			try
			{
				transactions = await _parser.ParseAsync(request.FileContent, request.Format, request.ImportSource, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to parse import file for user {UserId}", request.UserId);
				errors.Add(ex.Message);
				return new ImportResult { Errors = errors };
			}

			_logger.LogInformation("Parsed {TransactionCount} transactions for user {UserId}", transactions.Count, request.UserId);

			var accountType = request.ImportSource.ToAccountType();
			var normalized = transactions.Select(t => ApplySignRules(t, accountType, request.ImportSource)).ToList();

			if (normalized.Count == 0)
			{
				_logger.LogWarning("Import produced no transactions for user {UserId}", request.UserId);
				return new ImportResult { Errors = errors };
			}

			var minDate = normalized.Min(t => t.Date.Date);
			var maxDate = normalized.Max(t => t.Date.Date);
			var existing = await _expenseRepository.ListForUserInDateRange(request.UserId, minDate, maxDate, request.PaymentMethodId);
			var toCreate = ImportDuplicateFilter.FilterDuplicates(existing, normalized);
			toCreate = ImportFilterRules.RemoveTransfersAndPayments(toCreate);
			var skippedDuplicates = normalized.Count - toCreate.Count;

			_logger.LogInformation(
				"Import deduped for user {UserId}: {ToCreate} to create, {SkippedDuplicates} duplicates skipped ({DateRangeStart} to {DateRangeEnd}, paymentMethod={PaymentMethodId})",
				request.UserId, toCreate.Count, skippedDuplicates, minDate, maxDate, request.PaymentMethodId);

			var created = 0;
			foreach (var t in toCreate)
			{
				try
				{
					var model = new CreateExpenseModel
					{
						ExpenseDate = t.Date,
						Expense = t.Description ?? "",
						Amount = t.Amount,
						PaymentMethod = request.PaymentMethodId,
						Category = null,
						DatePaid = null,
						IsSplit = false,
						CreatedBy = ExpenseConstants.ImportCreatedBy
					};
					var expense = await _expenseRepository.Create(request.UserId, model);
					if (expense != null)
						created++;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to create expense from import row for user {UserId}: {Date} {Amount}", request.UserId, t.Date, t.Amount);
					errors.Add($"{t.Date:yyyy-MM-dd} {t.Amount}: {ex.Message}");
				}
			}

			_logger.LogInformation(
				"Import completed for user {UserId}: created={Created}, skippedDuplicates={SkippedDuplicates}, errors={ErrorCount}",
				request.UserId, created, skippedDuplicates, errors.Count);

			return new ImportResult
			{
				Created = created,
				SkippedDuplicates = skippedDuplicates,
				Errors = errors
			};
		}

		private static BankTransaction ApplySignRules(BankTransaction t, BankAccountType accountType, ImportSource importSource)
		{
			if (importSource == ImportSource.DiscoverSavings || importSource == ImportSource.DiscoverChecking || importSource == ImportSource.AbfcuSavings || importSource == ImportSource.AbfcuChecking)
				t.Amount = -t.Amount;
			return t;
		}
	}

	public class ImportFromFileCommandValidator : AbstractValidator<ImportFromFileCommand>
	{
		public ImportFromFileCommandValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.PaymentMethodId).GreaterThan(0);
			RuleFor(x => x.Format).NotEmpty()
				.Must(ImportFormat.IsCsv)
				.WithMessage($"Format must be {ImportFormat.Csv}.");
			RuleFor(x => x.FileContent).NotNull();
		}
	}
}
