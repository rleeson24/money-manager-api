using Moq;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Expenses;

public class BulkDeleteExpensesHandlerTests : HandlerBase<BulkDeleteExpensesHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly CancellationToken _ct;
	protected IReadOnlyList<int> _ids = null!;
	protected bool _result;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public BulkDeleteExpensesHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_ct = CancellationToken.None;
		_ids = new[] { 10, 20, 30 };
	}

	public class Success_Setup : BulkDeleteExpensesHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.BulkDelete(_ids, _userId)).ReturnsAsync(true);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new BulkDeleteExpensesCommand(_ids, _userId), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsTrue() => Assert.True(_fixture._result);
	}

	public class Failure_Setup : BulkDeleteExpensesHandlerTests
	{
		public Failure_Setup()
		{
			_repository.Setup(r => r.BulkDelete(_ids, _userId)).ReturnsAsync(false);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new BulkDeleteExpensesCommand(_ids, _userId), _ct);
		}
	}

	public class Failure : IClassFixture<Failure_Setup>
	{
		private readonly Failure_Setup _fixture;

		public Failure(Failure_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsFalse() => Assert.False(_fixture._result);
	}

	public class RepositoryThrows_Setup : BulkDeleteExpensesHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("bulk delete failed");
			_repository.Setup(r => r.BulkDelete(_ids, _userId)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new BulkDeleteExpensesCommand(_ids, _userId), _ct));
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
