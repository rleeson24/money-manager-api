using Moq;
using MoneyManager.Core.Application.ExpenseSplits.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.ExpenseSplits;

public class GetExpenseSplitsHandlerTests : HandlerBase<GetExpenseSplitsHandler>
{
	private Mock<IExpenseSplitRepository> _repository => MockFor<IExpenseSplitRepository>();

	protected readonly Guid _userId;
	protected readonly int _expenseId;
	protected readonly CancellationToken _ct;
	protected IReadOnlyList<ExpenseSplit> _result = null!;
	protected IReadOnlyList<ExpenseSplit> _splits = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public GetExpenseSplitsHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_expenseId = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
		_splits = Fixture.CreateMany<ExpenseSplit>(2).ToList();
	}

	public class Success_Setup : GetExpenseSplitsHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.GetByExpenseId(_expenseId, _userId)).ReturnsAsync(_splits);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetExpenseSplitsQuery(_expenseId, _userId), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsSplits() => Assert.Same(_fixture._splits, _fixture._result);
	}

	public class EmptyList_Setup : GetExpenseSplitsHandlerTests
	{
		public EmptyList_Setup()
		{
			_repository.Setup(r => r.GetByExpenseId(_expenseId, _userId))
				.ReturnsAsync(Array.Empty<ExpenseSplit>());
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetExpenseSplitsQuery(_expenseId, _userId), _ct);
		}
	}

	public class EmptyList : IClassFixture<EmptyList_Setup>
	{
		private readonly EmptyList_Setup _fixture;

		public EmptyList(EmptyList_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsEmptyList() => Assert.Empty(_fixture._result);
	}

	public class RepositoryThrows_Setup : GetExpenseSplitsHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("get splits failed");
			_repository.Setup(r => r.GetByExpenseId(_expenseId, _userId)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new GetExpenseSplitsQuery(_expenseId, _userId), _ct));
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
