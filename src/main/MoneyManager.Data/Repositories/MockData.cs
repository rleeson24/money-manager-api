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
			new Expense { Expense_I = 1, ExpenseDate = new DateTime(2026, 1, 19), ExpenseDescription = "COPA AIRLINES PANAMA PAN", Amount = 126.34m, PaymentMethod = 1, Category = 1, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 19, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 19, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 2, ExpenseDate = new DateTime(2026, 1, 22), ExpenseDescription = "Freddy's - custard", Amount = 5.51m, PaymentMethod = 1, Category = 2, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 3, ExpenseDate = new DateTime(2026, 1, 22), ExpenseDescription = "WALMART.COM - David birthday present - couch", Amount = 83.30m, PaymentMethod = 1, Category = 3, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 22, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 4, ExpenseDate = new DateTime(2026, 1, 23), ExpenseDescription = "Gas Station", Amount = 45.0m, PaymentMethod = 1, Category = 4, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 23, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 23, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 5, ExpenseDate = new DateTime(2026, 1, 24), ExpenseDescription = "Ross - return lita shoes", Amount = 21.79m, PaymentMethod = 1, Category = 1, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 24, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 24, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 6, ExpenseDate = new DateTime(2026, 1, 25), ExpenseDescription = "AMAZON - Luca Christmas gift", Amount = 15.99m, PaymentMethod = 1, Category = 8, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 25, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 25, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 7, ExpenseDate = new DateTime(2026, 1, 26), ExpenseDescription = "Pharmacy - Prescription", Amount = 32.5m, PaymentMethod = 1, Category = 5, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 8, ExpenseDate = new DateTime(2026, 1, 27), ExpenseDescription = "Groceries - Whole Foods", Amount = 125.5m, PaymentMethod = 1, Category = 6, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 27, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 27, 12, 0, 0, DateTimeKind.Utc) },
			new Expense { Expense_I = 9, ExpenseDate = new DateTime(2026, 1, 28), ExpenseDescription = "Outdoor Equipment", Amount = 89.25m, PaymentMethod = 1, Category = 7, DatePaid = null, CreatedDateTime = new DateTime(2026, 1, 28, 12, 0, 0, DateTimeKind.Utc), ModifiedDateTime = new DateTime(2026, 1, 28, 12, 0, 0, DateTimeKind.Utc) },
		};

		public static IReadOnlyList<Category> Categories { get; } = new List<Category>
		{
			new Category { Category_I = 1, Name = "Other Expenses (Pare)" },
			new Category { Category_I = 2, Name = "Dining/Eating Out" },
			new Category { Category_I = 3, Name = "Special Occasions (P)" },
			new Category { Category_I = 4, Name = "Gas - Auto" },
			new Category { Category_I = 5, Name = "Health" },
			new Category { Category_I = 6, Name = "Groceries (Parent)" },
			new Category { Category_I = 7, Name = "Outdoors (Parent)" },
			new Category { Category_I = 8, Name = "Gifts (Parent)" },
			new Category { Category_I = 9, Name = "Transportation" },
			new Category { Category_I = 10, Name = "Entertainment" },
			new Category { Category_I = 11, Name = "Utilities" },
			new Category { Category_I = 12, Name = "Healthcare" },
			new Category { Category_I = 13, Name = "Shopping" },
			new Category { Category_I = 14, Name = "Health & Fitness" },
			new Category { Category_I = 15, Name = "Housing" },
			new Category { Category_I = 16, Name = "Education" },
			new Category { Category_I = 17, Name = "Food" },
		};

		public static IReadOnlyList<PaymentMethod> PaymentMethods { get; } = new List<PaymentMethod>
		{
			new PaymentMethod { ID = 1, PaymentMethodName = "Discover" },
			new PaymentMethod { ID = 2, PaymentMethodName = "Visa" },
			new PaymentMethod { ID = 3, PaymentMethodName = "Mastercard" },
			new PaymentMethod { ID = 4, PaymentMethodName = "American Express" },
			new PaymentMethod { ID = 5, PaymentMethodName = "Debit Card" },
			new PaymentMethod { ID = 6, PaymentMethodName = "Cash" },
			new PaymentMethod { ID = 7, PaymentMethodName = "Bank Transfer" },
			new PaymentMethod { ID = 8, PaymentMethodName = "PayPal" },
		};
	}
}
