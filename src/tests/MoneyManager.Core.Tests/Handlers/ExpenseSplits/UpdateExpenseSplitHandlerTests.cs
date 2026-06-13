using Moq;
using MoneyManager.Core.Application.ExpenseSplits.Commands;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.ExpenseSplits;

public class UpdateExpenseSplitHandlerTests : HandlerBase<UpdateExpenseSplitHandler>
{
	private Mock<IExpenseRepository> _expenseRepository => MockFor<IExpenseRepository>();
	private Mock<IExpenseSplitRepository> _splitRepository => MockFor<IExpenseSplitRepository>();

	protected readonly Guid _userId;
	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected CreateOrUpdateExpenseSplitModel _model = null!;
	protected ExpenseSplit? _result;
	protected Expense _expense = null!;
	protected ExpenseSplit _split = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public UpdateExpenseSplitHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
		_model = new CreateOrUpdateExpenseSplitModel
		{
			Expense_I = Math.Abs(Fixture.Create<int>()) % 10000 + 1,
			Description = "Split item",
			Amount = 25.50m,
			Category = 1
		};
		_expense = Fixture.Create<Expense>();
		_expense.Expense_I = _model.Expense_I;
		_split = Fixture.Create<ExpenseSplit>();
		_split.Id = _id;
	}

	public class Success_Setup : UpdateExpenseSplitHandlerTests
	{
		public Success_Setup()
		{
			_expenseRepository.Setup(r => r.Get(_model.Expense_I, _userId)).ReturnsAsync(_expense);
			_splitRepository.Setup(r => r.Update(_id, _userId, _model)).ReturnsAsync(_split);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new UpdateExpenseSplitCommand(_id, _userId, _model), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsSplit() => Assert.Same(_fixture._split, _fixture._result);
	}

	public class ExpenseNotFound_Setup : UpdateExpenseSplitHandlerTests
	{
		public ExpenseNotFound_Setup()
		{
			_expenseRepository.Setup(r => r.Get(_model.Expense_I, _userId)).ReturnsAsync((Expense?)null);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new UpdateExpenseSplitCommand(_id, _userId, _model), _ct);
		}
	}

	public class ExpenseNotFound : IClassFixture<ExpenseNotFound_Setup>
	{
		private readonly ExpenseNotFound_Setup _fixture;

		public ExpenseNotFound(ExpenseNotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsNull() => Assert.Null(_fixture._result);

		[Fact]
		public void DoesNotCallUpdate()
		{
			_fixture._splitRepository.Verify(
				r => r.Update(It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<CreateOrUpdateExpenseSplitModel>()),
				Times.Never);
		}
	}

	public class SplitRepositoryThrows_Setup : UpdateExpenseSplitHandlerTests
	{
		public SplitRepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("update split failed");
			_expenseRepository.Setup(r => r.Get(_model.Expense_I, _userId)).ReturnsAsync(_expense);
			_splitRepository.Setup(r => r.Update(_id, _userId, _model)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new UpdateExpenseSplitCommand(_id, _userId, _model), _ct));
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
