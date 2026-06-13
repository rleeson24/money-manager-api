using Moq;
using MoneyManager.Core.Application.Import.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Import;

public class GetLastImportDatesHandlerTests : HandlerBase<GetLastImportDatesHandler>
{
	private Mock<IExpenseRepository> _repository => MockFor<IExpenseRepository>();

	protected readonly Guid _userId;
	protected readonly CancellationToken _ct;
	protected IReadOnlyList<int> _paymentMethodIds = null!;
	protected IReadOnlyList<LastImportDatesForPaymentMethod> _result = null!;
	protected IReadOnlyList<LastImportDatesForPaymentMethod> _dates = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public GetLastImportDatesHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_ct = CancellationToken.None;
		_paymentMethodIds = new[] { 1, 2, 3 };
		_dates = Fixture.CreateMany<LastImportDatesForPaymentMethod>(3).ToList();
	}

	public class Success_Setup : GetLastImportDatesHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.GetLastImportDates(_userId, _paymentMethodIds)).ReturnsAsync(_dates);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(
				new GetLastImportDatesQuery(_userId, _paymentMethodIds), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsDates() => Assert.Same(_fixture._dates, _fixture._result);

		[Fact]
		public void CallsRepositoryOnce()
		{
			_fixture._repository.Verify(
				r => r.GetLastImportDates(_fixture._userId, _fixture._paymentMethodIds),
				Times.Once);
		}
	}

	public class RepositoryThrows_Setup : GetLastImportDatesHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("dates failed");
			_repository.Setup(r => r.GetLastImportDates(_userId, _paymentMethodIds)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new GetLastImportDatesQuery(_userId, _paymentMethodIds), _ct));
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
