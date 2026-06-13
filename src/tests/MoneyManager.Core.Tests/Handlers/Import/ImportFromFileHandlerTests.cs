using Moq;
using MoneyManager.Core.Application.Import.Commands;
using MoneyManager.Core.Constants;
using MoneyManager.Core.Import;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Import;

public class ImportFromFileHandlerTests : HandlerBase<ImportFromFileHandler>
{
	private Mock<ITransactionFileParser> _parser => MockFor<ITransactionFileParser>();
	private Mock<IExpenseRepository> _expenseRepository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly CancellationToken _ct;
	protected readonly int _paymentMethodId;
	protected readonly ImportSource _importSource;
	protected readonly string _format;
	protected Stream _fileContent = null!;
	protected ImportResult _result = null!;
	protected IReadOnlyList<BankTransaction> _transactions = null!;
	protected Expense _createdExpense = null!;
	protected Exception _parseException = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public ImportFromFileHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_ct = CancellationToken.None;
		_paymentMethodId = Math.Abs(Fixture.Create<int>()) % 100 + 1;
		_importSource = ImportSource.Arvest;
		_format = ImportFormat.Csv;
		_fileContent = new MemoryStream();
		_createdExpense = Fixture.Create<Expense>();
		_transactions = new List<BankTransaction>
		{
			new()
			{
				Date = new DateTime(2025, 3, 15),
				Amount = -25.50m,
				Description = "Grocery store",
				AccountType = BankAccountType.Depository
			},
			new()
			{
				Date = new DateTime(2025, 3, 16),
				Amount = -10.00m,
				Description = "Coffee shop",
				AccountType = BankAccountType.Depository
			}
		};
		UseRealImportPipeline();
	}

	private void UseRealImportPipeline()
	{
		var duplicateFilter = new ImportDuplicateFilter();
		var transactionFilter = new ImportTransactionFilter();
		var normalizer = new ImportTransactionNormalizer();

		MockFor<IImportDuplicateFilter>()
			.Setup(f => f.FilterDuplicates(It.IsAny<IReadOnlyList<Expense>>(), It.IsAny<IReadOnlyList<BankTransaction>>()))
			.Returns((IReadOnlyList<Expense> existing, IReadOnlyList<BankTransaction> transactions) =>
				duplicateFilter.FilterDuplicates(existing, transactions));

		MockFor<IImportTransactionFilter>()
			.Setup(f => f.RemoveTransfersAndPayments(It.IsAny<IReadOnlyList<BankTransaction>>()))
			.Returns((IReadOnlyList<BankTransaction> transactions) =>
				transactionFilter.RemoveTransfersAndPayments(transactions));

		MockFor<IImportTransactionNormalizer>()
			.Setup(n => n.Normalize(It.IsAny<BankTransaction>(), It.IsAny<ImportSource>()))
			.Returns((BankTransaction transaction, ImportSource source) =>
				normalizer.Normalize(transaction, source));
	}

	public class Success_Setup : ImportFromFileHandlerTests
	{
		public Success_Setup()
		{
			_parser.Setup(p => p.ParseAsync(_fileContent, _format, _importSource, _ct)).ReturnsAsync(_transactions);
			_expenseRepository.Setup(r => r.ListForUserInDateRange(
					_userId,
					new DateTime(2025, 3, 15),
					new DateTime(2025, 3, 16),
					_paymentMethodId))
				.ReturnsAsync(Array.Empty<Expense>());
			_expenseRepository.Setup(r => r.Create(_userId, It.IsAny<CreateExpenseModel>())).ReturnsAsync(_createdExpense);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new ImportFromFileCommand(_userId, _fileContent, _format, _importSource, _paymentMethodId), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void CreatesAllTransactions() => Assert.Equal(2, _fixture._result.Created);

		[Fact]
		public void HasNoErrors() => Assert.Empty(_fixture._result.Errors);

		[Fact]
		public void SkipsNoDuplicates() => Assert.Equal(0, _fixture._result.SkippedDuplicates);
	}

	public class ParseFails_Setup : ImportFromFileHandlerTests
	{
		public ParseFails_Setup()
		{
			_parseException = new InvalidDataException("invalid csv");
			_parser.Setup(p => p.ParseAsync(_fileContent, _format, _importSource, _ct)).ThrowsAsync(_parseException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new ImportFromFileCommand(_userId, _fileContent, _format, _importSource, _paymentMethodId), _ct);
		}
	}

	public class ParseFails : IClassFixture<ParseFails_Setup>
	{
		private readonly ParseFails_Setup _fixture;

		public ParseFails(ParseFails_Setup fixture) => _fixture = fixture;

		[Fact]
		public void CreatesNothing() => Assert.Equal(0, _fixture._result.Created);

		[Fact]
		public void ReturnsParseError() => Assert.Contains(_fixture._parseException.Message, _fixture._result.Errors);

		[Fact]
		public void DoesNotQueryExistingExpenses()
		{
			_fixture._expenseRepository.Verify(
				r => r.ListForUserInDateRange(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()),
				Times.Never);
		}
	}

	public class EmptyTransactions_Setup : ImportFromFileHandlerTests
	{
		public EmptyTransactions_Setup()
		{
			_parser.Setup(p => p.ParseAsync(_fileContent, _format, _importSource, _ct))
				.ReturnsAsync(Array.Empty<BankTransaction>());
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new ImportFromFileCommand(_userId, _fileContent, _format, _importSource, _paymentMethodId), _ct);
		}
	}

	public class EmptyTransactions : IClassFixture<EmptyTransactions_Setup>
	{
		private readonly EmptyTransactions_Setup _fixture;

		public EmptyTransactions(EmptyTransactions_Setup fixture) => _fixture = fixture;

		[Fact]
		public void CreatesNothing() => Assert.Equal(0, _fixture._result.Created);
	}

	public class RepositoryThrows_Setup : ImportFromFileHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("date range failed");
			_parser.Setup(p => p.ParseAsync(_fileContent, _format, _importSource, _ct)).ReturnsAsync(_transactions);
			_expenseRepository.Setup(r => r.ListForUserInDateRange(
					_userId,
					new DateTime(2025, 3, 15),
					new DateTime(2025, 3, 16),
					_paymentMethodId))
				.ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(
					new ImportFromFileCommand(_userId, _fileContent, _format, _importSource, _paymentMethodId), _ct));
		}
	}

	public class RepositoryThrows : IClassFixture<RepositoryThrows_Setup>
	{
		private readonly RepositoryThrows_Setup _fixture;

		public RepositoryThrows(RepositoryThrows_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ThrowsExpectedException() => Assert.Same(_fixture._expectedException, _fixture._thrownException);
	}
}
