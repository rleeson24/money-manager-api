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
		ImportSource? ImportSource,
		int PaymentMethodId) : IRequest<ImportResult>;

	public class ImportFromFileHandler : IRequestHandler<ImportFromFileCommand, ImportResult>
	{
		private readonly ITransactionFileParser _parser;
		private readonly IExpenseRepository _expenseRepository;
		private readonly IImportDuplicateFilter _duplicateFilter;
		private readonly IImportTransactionFilter _transactionFilter;
		private readonly IImportTransactionNormalizer _transactionNormalizer;
		private readonly ILogger<ImportFromFileHandler> _logger;

		public ImportFromFileHandler(
			ITransactionFileParser parser,
			IExpenseRepository expenseRepository,
			IImportDuplicateFilter duplicateFilter,
			IImportTransactionFilter transactionFilter,
			IImportTransactionNormalizer transactionNormalizer,
			ILogger<ImportFromFileHandler> logger)
		{
			_parser = parser;
			_expenseRepository = expenseRepository;
			_duplicateFilter = duplicateFilter;
			_transactionFilter = transactionFilter;
			_transactionNormalizer = transactionNormalizer;
			_logger = logger;
		}

		public async Task<ImportResult> Handle(ImportFromFileCommand request, CancellationToken cancellationToken)
		{
			var importSource = request.ImportSource!.Value;
			_logger.LogInformation(
				"Starting import for user {UserId}: format={Format}, source={ImportSource}, paymentMethod={PaymentMethodId}",
				request.UserId, request.Format, importSource, request.PaymentMethodId);

			var errors = new List<string>();
			IReadOnlyList<BankTransaction> transactions;
			try
			{
				transactions = await _parser.ParseAsync(request.FileContent, request.Format, importSource, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to parse import file for user {UserId}", request.UserId);
				errors.Add(ex.Message);
				return new ImportResult { Errors = errors };
			}

			_logger.LogInformation("Parsed {TransactionCount} transactions for user {UserId}", transactions.Count, request.UserId);

			var normalized = transactions.Select(t => _transactionNormalizer.Normalize(t, importSource)).ToList();

			if (normalized.Count == 0)
			{
				_logger.LogWarning("Import produced no transactions for user {UserId}", request.UserId);
				return new ImportResult { Errors = errors };
			}

			var minDate = normalized.Min(t => t.Date.Date);
			var maxDate = normalized.Max(t => t.Date.Date);
			var existing = await _expenseRepository.ListForUserInDateRange(request.UserId, minDate, maxDate, request.PaymentMethodId);
			var toCreate = _duplicateFilter.FilterDuplicates(existing, normalized);
			toCreate = _transactionFilter.RemoveTransfersAndPayments(toCreate);
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

	}

	public class ImportFromFileCommandValidator : AbstractValidator<ImportFromFileCommand>
	{
		public ImportFromFileCommandValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.PaymentMethodId).GreaterThan(0).WithMessage("Payment method is required.");
			RuleFor(x => x.ImportSource).NotNull().WithMessage("Import source is required.");
			RuleFor(x => x.Format).NotEmpty().WithMessage("Format (CSV) is required.")
				.Must(ImportFormat.IsCsv)
				.WithMessage($"Format must be {ImportFormat.Csv}.");
			RuleFor(x => x.FileContent).NotNull();
		}
	}
}
