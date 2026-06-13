using Moq;
using MoneyManager.Core.Application.PaymentMethods.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.PaymentMethods;

public class GetPaymentMethodsHandlerTests : HandlerBase<GetPaymentMethodsHandler>
{
	private Mock<IPaymentMethodRepository> _repository => MockFor<IPaymentMethodRepository>();

	protected readonly CancellationToken _ct;
	protected IReadOnlyList<PaymentMethod>? _result;
	protected IReadOnlyList<PaymentMethod> _paymentMethods = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public GetPaymentMethodsHandlerTests()
	{
		_ct = CancellationToken.None;
		_paymentMethods = Fixture.CreateMany<PaymentMethod>(3).ToList();
	}

	public class Success_Setup : GetPaymentMethodsHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.GetAll()).ReturnsAsync(_paymentMethods);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetPaymentMethodsQuery(), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsPaymentMethods() => Assert.Same(_fixture._paymentMethods, _fixture._result);

		[Fact]
		public void CallsGetAllOnce() => _fixture._repository.Verify(r => r.GetAll(), Times.Once);
	}

	public class RepositoryThrows_Setup : GetPaymentMethodsHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("get all failed");
			_repository.Setup(r => r.GetAll()).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new GetPaymentMethodsQuery(), _ct));
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
