using Moq;
using MoneyManager.Core.Application.ExpenseSplits.Commands;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.ExpenseSplits;

public class ReplaceExpenseSplitsHandlerTests : HandlerBase<ReplaceExpenseSplitsHandler>
{
	private Mock<IExpenseRepository> _expenseRepository => MockFor<IExpenseRepository>();
	private Mock<IExpenseSplitRepository> _splitRepository => MockFor<IExpenseSplitRepository>();

	protected readonly Guid _userId;
	protected readonly int _expenseId;
	protected readonly CancellationToken _ct;
	protected ReplaceExpenseSplitsRequest _request = null!;
	protected ReplaceSplitsResult _result = null!;
	protected Expense _expense = null!;
	protected IReadOnlyList<ExpenseSplit> _splits = null!;
	protected string _validationError = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public ReplaceExpenseSplitsHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_expenseId = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
		_request = new ReplaceExpenseSplitsRequest
		{
			Splits =
			[
				new ReplaceExpenseSplitItemModel { Description = "Food", Amount = 60m, Category = 1 },
				new ReplaceExpenseSplitItemModel { Description = "Transport", Amount = 40m, Category = 2 }
			]
		};
		_expense = Fixture.Create<Expense>();
		_expense.Expense_I = _expenseId;
		_expense.Amount = 100m;
		_splits = Fixture.CreateMany<ExpenseSplit>(2).ToList();
	}

	public class Success_Setup : ReplaceExpenseSplitsHandlerTests
	{
		public Success_Setup()
		{
			_expenseRepository.Setup(r => r.Get(_expenseId, _userId)).ReturnsAsync(_expense);
			_splitRepository.Setup(r => r.ReplaceByExpenseId(_expenseId, _userId, _expense.Amount, _request.Splits))
				.ReturnsAsync(ReplaceSplitsResult.Success(_splits));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new ReplaceExpenseSplitsCommand(_expenseId, _userId, _request), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsSuccess() => Assert.True(_fixture._result.IsSuccess);

		[Fact]
		public void ReturnsSplits() => Assert.Same(_fixture._splits, _fixture._result.Splits);
	}

	public class ExpenseNotFound_Setup : ReplaceExpenseSplitsHandlerTests
	{
		public ExpenseNotFound_Setup()
		{
			_expenseRepository.Setup(r => r.Get(_expenseId, _userId)).ReturnsAsync((Expense?)null);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new ReplaceExpenseSplitsCommand(_expenseId, _userId, _request), _ct);
		}
	}

	public class ExpenseNotFound : IClassFixture<ExpenseNotFound_Setup>
	{
		private readonly ExpenseNotFound_Setup _fixture;

		public ExpenseNotFound(ExpenseNotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsFailure() => Assert.False(_fixture._result.IsSuccess);

		[Fact]
		public void ReturnsNotFoundMessage() => Assert.Equal("Expense not found.", _fixture._result.ValidationError);
	}

	public class ValidationFailure_Setup : ReplaceExpenseSplitsHandlerTests
	{
		public ValidationFailure_Setup()
		{
			_validationError = "Split amounts must sum to parent amount.";
			_expenseRepository.Setup(r => r.Get(_expenseId, _userId)).ReturnsAsync(_expense);
			_splitRepository.Setup(r => r.ReplaceByExpenseId(_expenseId, _userId, _expense.Amount, _request.Splits))
				.ReturnsAsync(ReplaceSplitsResult.Failure(_validationError));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new ReplaceExpenseSplitsCommand(_expenseId, _userId, _request), _ct);
		}
	}

	public class ValidationFailure : IClassFixture<ValidationFailure_Setup>
	{
		private readonly ValidationFailure_Setup _fixture;

		public ValidationFailure(ValidationFailure_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsValidationError() => Assert.Equal(_fixture._validationError, _fixture._result.ValidationError);
	}

	public class SplitRepositoryThrows_Setup : ReplaceExpenseSplitsHandlerTests
	{
		public SplitRepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("replace failed");
			_expenseRepository.Setup(r => r.Get(_expenseId, _userId)).ReturnsAsync(_expense);
			_splitRepository.Setup(r => r.ReplaceByExpenseId(_expenseId, _userId, _expense.Amount, _request.Splits))
				.ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new ReplaceExpenseSplitsCommand(_expenseId, _userId, _request), _ct));
		}
	}

	public class SplitRepositoryThrows : IClassFixture<SplitRepositoryThrows_Setup>
	{
		private readonly SplitRepositoryThrows_Setup _fixture;

		public SplitRepositoryThrows(SplitRepositoryThrows_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ThrowsExpectedException() => Assert.Same(_fixture._expectedException, _fixture._thrownException);
	}
}
