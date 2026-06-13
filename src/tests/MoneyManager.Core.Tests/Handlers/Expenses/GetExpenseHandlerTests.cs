using Moq;
using MoneyManager.Core.Application.Expenses.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Expenses;

public class GetExpenseHandlerTests : HandlerBase<GetExpenseHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected Expense? _result;
	protected Expense _expense = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public GetExpenseHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
		_expense = Fixture.Create<Expense>();
		_expense.Expense_I = _id;
	}

	public class Success_Setup : GetExpenseHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Get(_id, _userId)).ReturnsAsync(_expense);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetExpenseQuery(_id, _userId), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsExpense() => Assert.Same(_fixture._expense, _fixture._result);

		[Fact]
		public void CallsRepositoryOnce()
		{
			_fixture._repository.Verify(r => r.Get(_fixture._id, _fixture._userId), Times.Once);
		}
	}

	public class RepositoryReturnsNull_Setup : GetExpenseHandlerTests
	{
		public RepositoryReturnsNull_Setup()
		{
			_repository.Setup(r => r.Get(_id, _userId)).ReturnsAsync((Expense?)null);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetExpenseQuery(_id, _userId), _ct);
		}
	}

	public class RepositoryReturnsNull : IClassFixture<RepositoryReturnsNull_Setup>
	{
		private readonly RepositoryReturnsNull_Setup _fixture;

		public RepositoryReturnsNull(RepositoryReturnsNull_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsNull() => Assert.Null(_fixture._result);
	}

	public class RepositoryThrows_Setup : GetExpenseHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("repository failed");
			_repository.Setup(r => r.Get(_id, _userId)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new GetExpenseQuery(_id, _userId), _ct));
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
