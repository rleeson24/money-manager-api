using Moq;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Expenses;

public class UpdateExpenseHandlerTests : HandlerBase<UpdateExpenseHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected Expense _expense = null!;
	protected UpdateExpenseResult _result = null!;
	protected UpdateExpenseResult _successResult = null!;
	protected UpdateExpenseResult _notFoundResult = null!;
	protected UpdateExpenseResult _conflictResult = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public UpdateExpenseHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
		_expense = Fixture.Create<Expense>();
		_expense.Expense_I = _id;
		_successResult = UpdateExpenseResult.Success(_expense);
		_notFoundResult = UpdateExpenseResult.NotFound();
		_conflictResult = UpdateExpenseResult.Conflict(_expense);
	}

	public class Success_Setup : UpdateExpenseHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Update(_id, _userId, _expense)).ReturnsAsync(_successResult);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new UpdateExpenseCommand(_id, _userId, _expense), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsSuccess() => Assert.True(_fixture._result.IsSuccess);

		[Fact]
		public void ReturnsUpdatedExpense() => Assert.Same(_fixture._expense, _fixture._result.Updated);
	}

	public class NotFound_Setup : UpdateExpenseHandlerTests
	{
		public NotFound_Setup()
		{
			_repository.Setup(r => r.Update(_id, _userId, _expense)).ReturnsAsync(_notFoundResult);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new UpdateExpenseCommand(_id, _userId, _expense), _ct);
		}
	}

	public class NotFound : IClassFixture<NotFound_Setup>
	{
		private readonly NotFound_Setup _fixture;

		public NotFound(NotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsNotFound() => Assert.True(_fixture._result.IsNotFound);
	}

	public class Conflict_Setup : UpdateExpenseHandlerTests
	{
		public Conflict_Setup()
		{
			_repository.Setup(r => r.Update(_id, _userId, _expense)).ReturnsAsync(_conflictResult);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new UpdateExpenseCommand(_id, _userId, _expense), _ct);
		}
	}

	public class Conflict : IClassFixture<Conflict_Setup>
	{
		private readonly Conflict_Setup _fixture;

		public Conflict(Conflict_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsConflict() => Assert.True(_fixture._result.IsConflict);
	}

	public class RepositoryThrows_Setup : UpdateExpenseHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("update failed");
			_repository.Setup(r => r.Update(_id, _userId, _expense)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new UpdateExpenseCommand(_id, _userId, _expense), _ct));
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
