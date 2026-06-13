using Moq;
using MoneyManager.Core.Application.Expenses.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Expenses;

public class GetExpensesHandlerTests : HandlerBase<GetExpensesHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly CancellationToken _ct;
	protected IReadOnlyList<Expense>? _result;
	protected IReadOnlyList<Expense> _expenses = null!;
	protected int _paymentMethod;
	protected string _currency = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public GetExpensesHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_ct = CancellationToken.None;
		_expenses = Fixture.CreateMany<Expense>(3).ToList();
	}

	public class Success_Setup : GetExpensesHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.ListForUser(_userId, null)).ReturnsAsync(_expenses);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetExpensesQuery(_userId), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsExpenses() => Assert.Same(_fixture._expenses, _fixture._result);

		[Fact]
		public void CallsListForUser()
		{
			_fixture._repository.Verify(r => r.ListForUser(_fixture._userId, null), Times.Once);
			_fixture._repository.Verify(
				r => r.ListForUserWithFilters(It.IsAny<Guid>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<string?>()),
				Times.Never);
		}
	}

	public class SuccessWithFilters_Setup : GetExpensesHandlerTests
	{
		public SuccessWithFilters_Setup()
		{
			_paymentMethod = Math.Abs(Fixture.Create<int>()) % 100 + 1;
			_currency = "USD";
			_repository.Setup(r => r.ListForUserWithFilters(_userId, _paymentMethod, null, _currency))
				.ReturnsAsync(_expenses);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new GetExpensesQuery(_userId, PaymentMethod: _paymentMethod, Currency: _currency), _ct);
		}
	}

	public class SuccessWithFilters : IClassFixture<SuccessWithFilters_Setup>
	{
		private readonly SuccessWithFilters_Setup _fixture;

		public SuccessWithFilters(SuccessWithFilters_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsFilteredExpenses() => Assert.Same(_fixture._expenses, _fixture._result);

		[Fact]
		public void CallsListForUserWithFilters()
		{
			_fixture._repository.Verify(
				r => r.ListForUserWithFilters(_fixture._userId, _fixture._paymentMethod, null, _fixture._currency),
				Times.Once);
			_fixture._repository.Verify(r => r.ListForUser(It.IsAny<Guid>(), It.IsAny<string?>()), Times.Never);
		}
	}

	public class RepositoryThrows_Setup : GetExpensesHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("list failed");
			_repository.Setup(r => r.ListForUser(_userId, null)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new GetExpensesQuery(_userId), _ct));
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
