using MoneyManager.Core.Models.Input;
using MoneyManager.Data.Repositories;
using Xunit;

namespace MoneyManager.Data.Tests.Repositories;

public class InMemoryExpenseSplitRepositoryTests
{
	[Fact]
	public async Task ListByExpenseIds_ReturnsOnlyRequestedSplitsForTheUser()
	{
		var fixture = new InMemoryRepositoryFixture();
		var store = fixture.CreateStore();
		var expenses = fixture.CreateExpenseRepository(store);
		var splits = fixture.CreateExpenseSplitRepository(store);
		var userId = fixture.SeedUserId;

		var first = await expenses.Create(userId, Expense("Groceries", 30m));
		var second = await expenses.Create(userId, Expense("Dining", 20m));
		Assert.NotNull(first);
		Assert.NotNull(second);

		await splits.Create(userId, Split(first!.Expense_I, 18m, 6, "Food"));
		await splits.Create(userId, Split(first.Expense_I, 12m, 21, "Dinner"));
		await splits.Create(userId, Split(second!.Expense_I, 20m, 15, "Shirt"));

		var result = await splits.ListByExpenseIds(userId, new[] { first.Expense_I });

		Assert.Equal(2, result.Count);
		Assert.All(result, split => Assert.Equal(first.Expense_I, split.Expense_I));
		Assert.Empty(await splits.ListByExpenseIds(userId, Array.Empty<int>()));
		Assert.Empty(await splits.ListByExpenseIds(fixture.OtherUserId, new[] { first.Expense_I }));
		Assert.Empty(await splits.ListByExpenseIds(userId, new[] { 999999 }));
	}

	private static CreateExpenseModel Expense(string name, decimal amount) =>
		new()
		{
			ExpenseDate = new DateTime(2026, 1, 15),
			Expense = name,
			Amount = amount,
			Category = 19,
			IsSplit = true
		};

	private static CreateOrUpdateExpenseSplitModel Split(int expenseId, decimal amount, int category, string description) =>
		new()
		{
			Expense_I = expenseId,
			Amount = amount,
			Category = category,
			Description = description
		};
}
