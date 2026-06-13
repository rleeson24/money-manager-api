using Moq;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Expenses;

public class DeleteExpenseHandlerTests : HandlerBase<DeleteExpenseHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected bool _result;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public DeleteExpenseHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
	}

	public class Success_Setup : DeleteExpenseHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Delete(_id, _userId)).ReturnsAsync(true);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new DeleteExpenseCommand(_id, _userId), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsTrue() => Assert.True(_fixture._result);

		[Fact]
		public void CallsRepositoryOnce()
		{
			_fixture._repository.Verify(r => r.Delete(_fixture._id, _fixture._userId), Times.Once);
		}
	}

	public class NotFound_Setup : DeleteExpenseHandlerTests
	{
		public NotFound_Setup()
		{
			_repository.Setup(r => r.Delete(_id, _userId)).ReturnsAsync(false);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new DeleteExpenseCommand(_id, _userId), _ct);
		}
	}

	public class NotFound : IClassFixture<NotFound_Setup>
	{
		private readonly NotFound_Setup _fixture;

		public NotFound(NotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsFalse() => Assert.False(_fixture._result);
	}

	public class RepositoryThrows_Setup : DeleteExpenseHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("delete failed");
			_repository.Setup(r => r.Delete(_id, _userId)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new DeleteExpenseCommand(_id, _userId), _ct));
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
