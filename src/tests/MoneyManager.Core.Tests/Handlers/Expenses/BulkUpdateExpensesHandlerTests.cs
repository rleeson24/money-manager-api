using Moq;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Expenses;

public class BulkUpdateExpensesHandlerTests : HandlerBase<BulkUpdateExpensesHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly CancellationToken _ct;
	protected IReadOnlyList<int> _ids = null!;
	protected Dictionary<string, object?> _updates = null!;
	protected bool _result;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public BulkUpdateExpensesHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_ct = CancellationToken.None;
		_ids = new[] { 1, 2, 3 };
		_updates = new Dictionary<string, object?> { [ExpenseFieldNames.Category] = 5 };
	}

	public class Success_Setup : BulkUpdateExpensesHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.BulkUpdate(_ids, _userId, _updates)).ReturnsAsync(true);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new BulkUpdateExpensesCommand(_ids, _userId, _updates), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsTrue() => Assert.True(_fixture._result);

		[Fact]
		public void CallsBulkUpdate()
		{
			_fixture._repository.Verify(r => r.BulkUpdate(_fixture._ids, _fixture._userId, _fixture._updates), Times.Once);
		}
	}

	public class Failure_Setup : BulkUpdateExpensesHandlerTests
	{
		public Failure_Setup()
		{
			_repository.Setup(r => r.BulkUpdate(_ids, _userId, _updates)).ReturnsAsync(false);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new BulkUpdateExpensesCommand(_ids, _userId, _updates), _ct);
		}
	}

	public class Failure : IClassFixture<Failure_Setup>
	{
		private readonly Failure_Setup _fixture;

		public Failure(Failure_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsFalse() => Assert.False(_fixture._result);
	}

	public class RepositoryThrows_Setup : BulkUpdateExpensesHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("bulk update failed");
			_repository.Setup(r => r.BulkUpdate(_ids, _userId, _updates)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new BulkUpdateExpensesCommand(_ids, _userId, _updates), _ct));
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
