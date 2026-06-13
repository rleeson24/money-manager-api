using Microsoft.Extensions.Options;
using Moq;
using MoneyManager.Core;
using MoneyManager.Core.Constants;
using MoneyManager.Data;
using MoneyManager.Data.Repositories;

namespace MoneyManager.Data.Tests.Repositories;

public class InMemoryRepositoryFixture
{
	public DateTime FixedUtcNow { get; } = new(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);

	public Guid SeedUserId { get; } = Guid.Parse(AspireConstants.DefaultSeedUserId);

	public Guid OtherUserId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

	public InMemoryStore CreateStore(string? aspireSeedUserId = null)
	{
		var options = Options.Create(new DataOptions
		{
			AspireSeedUserId = aspireSeedUserId ?? SeedUserId.ToString()
		});
		return new InMemoryStore(options);
	}

	public Mock<INowProvider> CreateNowProviderMock(DateTime? utcNow = null)
	{
		var mock = new Mock<INowProvider>();
		mock.Setup(n => n.UtcNow).Returns(utcNow ?? FixedUtcNow);
		return mock;
	}

	public INowProvider CreateNowProvider(DateTime? utcNow = null) =>
		CreateNowProviderMock(utcNow).Object;

	public InMemoryExpenseRepository CreateExpenseRepository(InMemoryStore? store = null, INowProvider? nowProvider = null) =>
		new(store ?? CreateStore(), nowProvider ?? CreateNowProvider());

	public InMemoryCategoryRepository CreateCategoryRepository(InMemoryStore? store = null) =>
		new(store ?? CreateStore());

	public InMemoryExpenseSplitRepository CreateExpenseSplitRepository(InMemoryStore? store = null) =>
		new(store ?? CreateStore());

	public InMemoryPaymentMethodRepository CreatePaymentMethodRepository(InMemoryStore? store = null) =>
		new(store ?? CreateStore());
}
