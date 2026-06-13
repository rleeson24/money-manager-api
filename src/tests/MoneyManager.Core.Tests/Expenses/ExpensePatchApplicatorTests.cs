using AutoFixture;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Expenses;

public class ExpensePatchApplicatorTests
{
	private readonly IFixture _fixture = ConfiguredFixture.Create();

	[Fact]
	public void Apply_ReturnsNewExpenseWithUpdatesApplied()
	{
		var current = _fixture.Create<Expense>();
		current.ExpenseDescription = "Original";
		current.Amount = 50m;
		var modifiedUtc = new DateTime(2025, 6, 10, 12, 0, 0, DateTimeKind.Utc);
		var updates = new Dictionary<string, object?>
		{
			[ExpenseFieldNames.Expense] = "Updated",
			[ExpenseFieldNames.Amount] = 75.25m
		};

		var result = ExpensePatchApplicator.Apply(current, updates, modifiedUtc);

		Assert.NotSame(current, result);
		Assert.Equal(current.Expense_I, result.Expense_I);
		Assert.Equal("Updated", result.ExpenseDescription);
		Assert.Equal(75.25m, result.Amount);
		Assert.Equal(modifiedUtc, result.ModifiedDateTime);
	}

	[Fact]
	public void ApplyTo_SetsNullableFieldsToNull()
	{
		var target = _fixture.Create<Expense>();
		target.PaymentMethod = 5;
		target.Category = 10;
		target.DatePaid = new DateTime(2025, 1, 15);
		var modifiedUtc = new DateTime(2025, 6, 10, 12, 0, 0, DateTimeKind.Utc);
		var updates = new Dictionary<string, object?>
		{
			[ExpenseFieldNames.PaymentMethod] = null,
			[ExpenseFieldNames.Category] = null,
			[ExpenseFieldNames.DatePaid] = null
		};

		ExpensePatchApplicator.ApplyTo(target, updates, modifiedUtc);

		Assert.Null(target.PaymentMethod);
		Assert.Null(target.Category);
		Assert.Null(target.DatePaid);
		Assert.Equal(modifiedUtc, target.ModifiedDateTime);
	}

	[Fact]
	public void ApplyTo_UpdatesBooleanFlags()
	{
		var target = _fixture.Create<Expense>();
		target.IsSplit = false;
		target.ExcludeFromCredit = false;
		var modifiedUtc = new DateTime(2025, 6, 10, 12, 0, 0, DateTimeKind.Utc);
		var updates = new Dictionary<string, object?>
		{
			[ExpenseFieldNames.IsSplit] = true,
			[ExpenseFieldNames.ExcludeFromCredit] = true
		};

		ExpensePatchApplicator.ApplyTo(target, updates, modifiedUtc);

		Assert.True(target.IsSplit);
		Assert.True(target.ExcludeFromCredit);
	}
}
