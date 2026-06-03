using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Import;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Import
{
	public interface IImportFromFileUseCase
	{
		Task<ImportResult> ExecuteAsync(Guid userId, Stream fileContent, string format, ImportSource importSource, int paymentMethodId, CancellationToken cancellationToken = default);
	}

	/// <summary>
	/// Imports transactions from an uploaded file: parse, dedupe, create expenses with CreatedBy=userId.
	/// </summary>
	public class ImportFromFileUseCase : IImportFromFileUseCase
	{
		private readonly ITransactionFileParser _parser;
		private readonly IExpenseRepository _expenseRepository;
		private readonly ILogger<ImportFromFileUseCase> _logger;

		public ImportFromFileUseCase(
			ITransactionFileParser parser,
			IExpenseRepository expenseRepository,
			ILogger<ImportFromFileUseCase> logger)
		{
			_parser = parser;
			_expenseRepository = expenseRepository;
			_logger = logger;
		}

		public async Task<ImportResult> ExecuteAsync(Guid userId, Stream fileContent, string format, ImportSource importSource, int paymentMethodId, CancellationToken cancellationToken = default)
		{
			_logger.LogInformation(
				"Starting import for user {UserId}: format={Format}, source={ImportSource}, paymentMethod={PaymentMethodId}",
				userId, format, importSource, paymentMethodId);

			var errors = new List<string>();
			IReadOnlyList<BankTransaction> transactions;
			try
			{
				transactions = await _parser.ParseAsync(fileContent, format, importSource, cancellationToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to parse import file for user {UserId}", userId);
				errors.Add(ex.Message);
				return new ImportResult { Errors = errors };
			}

			_logger.LogInformation("Parsed {TransactionCount} transactions for user {UserId}", transactions.Count, userId);

			var accountType = importSource.ToAccountType();
			var normalized = transactions.Select(t => ApplySignRules(t, accountType, importSource)).ToList();

			if (normalized.Count == 0)
			{
				_logger.LogWarning("Import produced no transactions for user {UserId}", userId);
				return new ImportResult { Errors = errors };
			}

			var minDate = normalized.Min(t => t.Date.Date);
			var maxDate = normalized.Max(t => t.Date.Date);
			var existing = await _expenseRepository.ListForUserInDateRange(userId, minDate, maxDate, paymentMethodId);
			var toCreate = ImportDuplicateFilter.FilterDuplicates(existing, normalized);
			toCreate = RemoveTransfersAndPayments(toCreate);
			var skippedDuplicates = normalized.Count - toCreate.Count;

			_logger.LogInformation(
				"Import deduped for user {UserId}: {ToCreate} to create, {SkippedDuplicates} duplicates skipped ({DateRangeStart} to {DateRangeEnd}, paymentMethod={PaymentMethodId})",
				userId, toCreate.Count, skippedDuplicates, minDate, maxDate, paymentMethodId);

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
						PaymentMethod = paymentMethodId,
						Category = null,
						DatePaid = null,
						IsSplit = false,
						CreatedBy = "Import"
					};
					var expense = await _expenseRepository.Create(userId, model);
					if (expense != null)
						created++;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to create expense from import row for user {UserId}: {Date} {Amount}", userId, t.Date, t.Amount);
					errors.Add($"{t.Date:yyyy-MM-dd} {t.Amount}: {ex.Message}");
				}
			}

			_logger.LogInformation(
				"Import completed for user {UserId}: created={Created}, skippedDuplicates={SkippedDuplicates}, errors={ErrorCount}",
				userId, created, skippedDuplicates, errors.Count);

			return new ImportResult
			{
				Created = created,
				SkippedDuplicates = skippedDuplicates,
				Errors = errors
			};
		}

		private IReadOnlyList<BankTransaction> RemoveTransfersAndPayments(IReadOnlyList<BankTransaction> toCreate)
		{
			return toCreate.Where(t =>
				!t.Description.Contains("INTERNET PAYMENT - THANK YOU", StringComparison.OrdinalIgnoreCase) &&
				!t.Description.Contains("EDI PYMNTS", StringComparison.OrdinalIgnoreCase) &&
				!t.Description.Contains("Discover (CONA)  NET/MOBILE ROBERT LEESON", StringComparison.OrdinalIgnoreCase)
			).ToList();
		}

		private static BankTransaction ApplySignRules(BankTransaction t, BankAccountType accountType, ImportSource importSource)
		{
			if (importSource == ImportSource.DiscoverSavings || importSource == ImportSource.DiscoverChecking || importSource == ImportSource.AbfcuSavings || importSource == ImportSource.AbfcuChecking)
			{
				// Depository: debits positive, credits negative (typical CSV: DEBIT has negative amount).
				t.Amount = -t.Amount;
			}

			// CreditCard: purchases positive, payments/returns negative.
			// Parser may already output correct signs; leave as-is for now.
			return t;
		}
	}
}
