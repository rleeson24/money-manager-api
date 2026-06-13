using Moq;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Expenses;

public class CreateExpenseHandlerTests : HandlerBase<CreateExpenseHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly CancellationToken _ct;
	protected CreateExpenseModel _model = null!;
	protected Expense? _result;
	protected Expense _createdExpense = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public CreateExpenseHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_ct = CancellationToken.None;
		_model = Fixture.Create<CreateExpenseModel>();
		_model.CreatedBy = null;
		_createdExpense = Fixture.Create<Expense>();
	}

	public class Success_Setup : CreateExpenseHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Create(_userId, _model)).ReturnsAsync(_createdExpense);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new CreateExpenseCommand(_userId, _model), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsCreatedExpense() => Assert.Same(_fixture._createdExpense, _fixture._result);

		[Fact]
		public void SetsCreatedByFromUserId()
		{
			Assert.Equal(_fixture._userId.ToString(), _fixture._model.CreatedBy);
			_fixture._repository.Verify(r => r.Create(_fixture._userId, _fixture._model), Times.Once);
		}
	}

	public class RepositoryReturnsNull_Setup : CreateExpenseHandlerTests
	{
		public RepositoryReturnsNull_Setup()
		{
			_repository.Setup(r => r.Create(_userId, _model)).ReturnsAsync((Expense?)null);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new CreateExpenseCommand(_userId, _model), _ct);
		}
	}

	public class RepositoryReturnsNull : IClassFixture<RepositoryReturnsNull_Setup>
	{
		private readonly RepositoryReturnsNull_Setup _fixture;

		public RepositoryReturnsNull(RepositoryReturnsNull_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsNull() => Assert.Null(_fixture._result);
	}

	public class RepositoryThrows_Setup : CreateExpenseHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("create failed");
			_repository.Setup(r => r.Create(_userId, _model)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new CreateExpenseCommand(_userId, _model), _ct));
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
