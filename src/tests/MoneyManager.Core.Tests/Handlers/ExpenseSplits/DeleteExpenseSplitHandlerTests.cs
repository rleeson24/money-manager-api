using Moq;
using MoneyManager.Core.Application.ExpenseSplits.Commands;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.ExpenseSplits;

public class DeleteExpenseSplitHandlerTests : HandlerBase<DeleteExpenseSplitHandler>
{
	private Mock<IExpenseSplitRepository> _repository => MockFor<IExpenseSplitRepository>();

	protected readonly Guid _userId;
	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected bool _result;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public DeleteExpenseSplitHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
	}

	public class Success_Setup : DeleteExpenseSplitHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Delete(_id, _userId)).ReturnsAsync(true);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new DeleteExpenseSplitCommand(_id, _userId), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsTrue() => Assert.True(_fixture._result);
	}

	public class NotFound_Setup : DeleteExpenseSplitHandlerTests
	{
		public NotFound_Setup()
		{
			_repository.Setup(r => r.Delete(_id, _userId)).ReturnsAsync(false);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new DeleteExpenseSplitCommand(_id, _userId), _ct);
		}
	}

	public class NotFound : IClassFixture<NotFound_Setup>
	{
		private readonly NotFound_Setup _fixture;

		public NotFound(NotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsFalse() => Assert.False(_fixture._result);
	}

	public class RepositoryThrows_Setup : DeleteExpenseSplitHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("delete split failed");
			_repository.Setup(r => r.Delete(_id, _userId)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new DeleteExpenseSplitCommand(_id, _userId), _ct));
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
