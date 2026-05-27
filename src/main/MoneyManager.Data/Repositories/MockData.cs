using System.Collections.Generic;
using MoneyManager.Core.Models;

namespace MoneyManager.Data.Repositories
{
	/// <summary>
	/// Static mock data matching the client mocks (expenseService, categoryService, paymentMethodService).
	/// Use for reference, seed data, or in-memory repository implementations.
	/// </summary>
	public static class MockData
	{
		public static IReadOnlyList<Expense> Expenses { get; } = new List<Expense>
		{
			new Expense { Expense_I = 1, ExpenseDate = new DateTime(2026, 1, 19), ExpenseDescription = "COPA AIRLINES PANAMA PAN", Amount = 126.34m, PaymentMethod = 1, Category = 42, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 19, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 19, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 2, ExpenseDate = new DateTime(2026, 1, 22), ExpenseDescription = "Freddy's - custard", Amount = 5.51m, PaymentMethod = 1, Category = 19, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc), IsSplit = true },
			new Expense { Expense_I = 3, ExpenseDate = new DateTime(2026, 1, 22), ExpenseDescription = "WALMART.COM - David birthday present - couch", Amount = 83.30m, PaymentMethod = 1, Category = 96, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 4, ExpenseDate = new DateTime(2026, 1, 23), ExpenseDescription = "Gas Station", Amount = 45.0m, PaymentMethod = 1, Category = 18, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 23, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 23, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 5, ExpenseDate = new DateTime(2026, 1, 24), ExpenseDescription = "Ross - return lita shoes", Amount = 21.79m, PaymentMethod = 1, Category = 81, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 24, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 24, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 6, ExpenseDate = new DateTime(2026, 1, 25), ExpenseDescription = "AMAZON - Luca Christmas gift", Amount = 15.99m, PaymentMethod = 1, Category = 96, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 25, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 25, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 7, ExpenseDate = new DateTime(2026, 1, 26), ExpenseDescription = "Pharmacy - Prescription", Amount = 32.5m, PaymentMethod = 1, Category = 48, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 8, ExpenseDate = new DateTime(2026, 1, 27), ExpenseDescription = "Groceries - Whole Foods", Amount = 125.5m, PaymentMethod = 1, Category = 6, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 27, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 27, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 9, ExpenseDate = new DateTime(2026, 1, 28), ExpenseDescription = "Outdoor Equipment", Amount = 89.25m, PaymentMethod = 1, Category = 111, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 28, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 28, 12, 0, 0, DateTimeKind.Utc) },
		};

		public static IReadOnlyList<Category> Categories => LegacyCategorySeed.Categories;

		public static IReadOnlyList<PaymentMethod> PaymentMethods => LegacyPaymentMethodSeed.PaymentMethods;

		public static List<ExpenseSplit> ExpenseSplits { get; } = new List<ExpenseSplit>();
	}
}
