using MoneyManager.Data.Repositories;
using Xunit;

namespace MoneyManager.Data.Tests.Repositories;

public class InMemoryPaymentMethodRepositoryTests
{
	[Fact]
	public async Task GetAll_ReturnsSeededPaymentMethods()
	{
		var repository = new InMemoryRepositoryFixture().CreatePaymentMethodRepository();

		var methods = await repository.GetAll();

		Assert.Equal(7, methods.Count);
		Assert.Contains(methods, m => m.ID == 1 && m.PaymentMethodName == "Discover Checking");
		Assert.Contains(methods, m => m.ID == 7 && m.PaymentMethodName == "Bank Transfer");
	}
}
