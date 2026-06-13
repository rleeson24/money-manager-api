using Moq;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Expenses;

public class PatchExpenseHandlerTests : HandlerBase<PatchExpenseHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected Dictionary<string, object?> _updates = null!;
	protected DateTime? _expectedModified;
	protected UpdateExpenseResult _result = null!;
	protected Expense _expense = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public PatchExpenseHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
		_expectedModified = Fixture.Create<DateTime>();
		_updates = new Dictionary<string, object?> { [ExpenseFieldNames.Amount] = 42.50m };
		_expense = Fixture.Create<Expense>();
	}

	public class Success_Setup : PatchExpenseHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Patch(_id, _userId, _updates, _expectedModified))
				.ReturnsAsync(UpdateExpenseResult.Success(_expense));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new PatchExpenseCommand(_id, _userId, _updates, _expectedModified), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsSuccess() => Assert.True(_fixture._result.IsSuccess);
	}

	public class NotFound_Setup : PatchExpenseHandlerTests
	{
		public NotFound_Setup()
		{
			_repository.Setup(r => r.Patch(_id, _userId, _updates, _expectedModified))
				.ReturnsAsync(UpdateExpenseResult.NotFound());
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new PatchExpenseCommand(_id, _userId, _updates, _expectedModified), _ct);
		}
	}

	public class NotFound : IClassFixture<NotFound_Setup>
	{
		private readonly NotFound_Setup _fixture;

		public NotFound(NotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsNotFound() => Assert.True(_fixture._result.IsNotFound);
	}

	public class Conflict_Setup : PatchExpenseHandlerTests
	{
		public Conflict_Setup()
		{
			_repository.Setup(r => r.Patch(_id, _userId, _updates, _expectedModified))
				.ReturnsAsync(UpdateExpenseResult.Conflict(_expense));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new PatchExpenseCommand(_id, _userId, _updates, _expectedModified), _ct);
		}
	}

	public class Conflict : IClassFixture<Conflict_Setup>
	{
		private readonly Conflict_Setup _fixture;

		public Conflict(Conflict_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsConflict() => Assert.True(_fixture._result.IsConflict);
	}

	public class RepositoryThrows_Setup : PatchExpenseHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("patch failed");
			_repository.Setup(r => r.Patch(_id, _userId, _updates, _expectedModified))
				.ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new PatchExpenseCommand(_id, _userId, _updates, _expectedModified), _ct));
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
