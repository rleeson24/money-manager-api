using MoneyManager.Core.Models;
using MoneyManager.Core.Reports;
using Xunit;

namespace MoneyManager.Core.Tests.Reports;

public class BudgetReportBuilderTests
{
	private static readonly IReadOnlyList<ExpenseSplit> NoSplits = Array.Empty<ExpenseSplit>();

	[Fact]
	public void EmptyMonth_HasNoSpending()
	{
		var report = BudgetReportBuilder.Build("2026-01", Array.Empty<Expense>(), NoSplits, Array.Empty<Category>());

		Assert.Equal("2026-01", report.Month);
		Assert.Equal(0m, report.TotalSpent);
		Assert.Equal(0, report.TransactionCount);
		Assert.Empty(report.Categories);
	}

	[Fact]
	public void GroupsChildrenUnderParent_AndSortsSpendingByAmount()
	{
		var expenses = new[]
		{
			Expense(1, 30m, 52),
			Expense(2, 20m, 61),
			Expense(3, 80m, 6)
		};
		var categories = new[]
		{
			Cat(4, "Housing"),
			Cat(52, "Electricity", 4),
			Cat(61, "Water", 4),
			Cat(6, "Groceries")
		};

		var report = BudgetReportBuilder.Build("2026-01", expenses, NoSplits, categories);

		Assert.Equal(130m, report.TotalSpent);
		Assert.Equal(3, report.TransactionCount);
		Assert.Equal(new[] { "Groceries", "Housing" }, report.Categories.Select(c => c.Name).ToArray());

		var groceries = report.Categories[0];
		Assert.Equal(80m, groceries.Amount);
		Assert.Equal(61.5m, groceries.PercentOfTotal);
		Assert.Equal(1, groceries.TransactionCount);
		Assert.Empty(groceries.Lines);

		var housing = report.Categories[1];
		Assert.Equal(50m, housing.Amount);
		Assert.Equal(38.5m, housing.PercentOfTotal);
		Assert.Equal(2, housing.TransactionCount);
		Assert.Equal(new[] { "Electricity", "Water" }, housing.Lines.Select(l => l.Name).ToArray());
		Assert.Equal(30m, housing.Lines[0].Amount);
		Assert.Equal(23.1m, housing.Lines[0].PercentOfTotal);
		Assert.Equal(20m, housing.Lines[1].Amount);
		Assert.Equal(15.4m, housing.Lines[1].PercentOfTotal);
	}

	[Fact]
	public void DirectParentSpending_AppearsAsItsOwnLine()
	{
		var expenses = new[]
		{
			Expense(1, 100m, 4),
			Expense(2, 40m, 52)
		};
		var categories = new[]
		{
			Cat(4, "Housing"),
			Cat(52, "Electricity", 4)
		};

		var housing = Assert.Single(BudgetReportBuilder.Build("2026-03", expenses, NoSplits, categories).Categories);

		Assert.Equal(140m, housing.Amount);
		Assert.Equal(2, housing.TransactionCount);
		Assert.Equal(2, housing.Lines.Count);
		Assert.Equal("Housing", housing.Lines[0].Name);
		Assert.True(housing.Lines[0].IsDirect);
		Assert.Equal(100m, housing.Lines[0].Amount);
		Assert.Equal("Electricity", housing.Lines[1].Name);
		Assert.False(housing.Lines[1].IsDirect);
	}

	[Fact]
	public void SplitExpense_UsesSplitCategories_NotTheParentAmount()
	{
		var expenses = new[]
		{
			Expense(1, 50m, 19, isSplit: true),
			Expense(2, 10m, 6)
		};
		var splits = new[]
		{
			Split(1, 30m, 6),
			Split(1, 20m, 21)
		};
		var categories = new[]
		{
			Cat(19, "Split"),
			Cat(6, "Groceries"),
			Cat(7, "Entertainment"),
			Cat(21, "Dining/Eating Out", 7)
		};

		var report = BudgetReportBuilder.Build("2026-01", expenses, splits, categories);

		Assert.Equal(60m, report.TotalSpent);
		Assert.Equal(3, report.TransactionCount);
		Assert.DoesNotContain(report.Categories, c => c.Name == "Split");
		Assert.Equal("Groceries", report.Categories[0].Name);
		Assert.Equal(40m, report.Categories[0].Amount);
		Assert.Equal(2, report.Categories[0].TransactionCount);

		var entertainment = report.Categories[1];
		Assert.Equal("Entertainment", entertainment.Name);
		Assert.Equal(20m, entertainment.Amount);
		var dining = Assert.Single(entertainment.Lines);
		Assert.Equal("Dining/Eating Out", dining.Name);
		Assert.Equal(20m, dining.Amount);
	}

	[Fact]
	public void SplitExpenseWithoutLines_UsesTheParentExpense()
	{
		var expenses = new[] { Expense(1, 15m, 6, isSplit: true) };
		var categories = new[] { Cat(6, "Groceries") };

		var groceries = Assert.Single(BudgetReportBuilder.Build("2026-01", expenses, NoSplits, categories).Categories);

		Assert.Equal(15m, groceries.Amount);
		Assert.Equal(1, groceries.TransactionCount);
	}

	[Fact]
	public void NonSplitExpense_IgnoresStraySplitLines()
	{
		var expenses = new[] { Expense(1, 15m, 6) };
		var splits = new[] { Split(1, 15m, 21) };
		var categories = new[]
		{
			Cat(6, "Groceries"),
			Cat(21, "Dining/Eating Out", 7),
			Cat(7, "Entertainment")
		};

		var report = BudgetReportBuilder.Build("2026-01", expenses, splits, categories);

		var groceries = Assert.Single(report.Categories);
		Assert.Equal("Groceries", groceries.Name);
		Assert.Equal(15m, groceries.Amount);
	}

	[Fact]
	public void Uncategorized_Unknown_AndOrphanChild_AreReported()
	{
		var expenses = new[]
		{
			Expense(1, 12m, null),
			Expense(2, 8m, 999),
			Expense(3, 5m, 18)
		};
		var categories = new[] { Cat(18, "Gas - Car", 5) };

		var report = BudgetReportBuilder.Build("2026-01", expenses, NoSplits, categories);

		Assert.Equal(25m, report.TotalSpent);
		Assert.Equal(
			new[] { "Uncategorized", "Unknown category (999)", "Gas - Car" },
			report.Categories.Select(c => c.Name).ToArray());
		Assert.Null(report.Categories[0].CategoryId);
		Assert.Equal(999, report.Categories[1].CategoryId);
		Assert.Empty(report.Categories[2].Lines);
	}

	[Fact]
	public void RefundsReduceTheCategory_AndIncomeIsNotPartOfSpending()
	{
		var expenses = new[]
		{
			Expense(1, 100m, 6),
			Expense(2, -25m, 6),
			Expense(3, -1000m, 56)
		};
		var categories = new[]
		{
			Cat(6, "Groceries"),
			Cat(56, "Other Income")
		};

		var report = BudgetReportBuilder.Build("2026-01", expenses, NoSplits, categories);

		Assert.Equal(75m, report.TotalSpent);
		Assert.Equal(3, report.TransactionCount);
		Assert.Equal("Groceries", report.Categories[0].Name);
		Assert.Equal(75m, report.Categories[0].Amount);
		Assert.Equal(100m, report.Categories[0].PercentOfTotal);
		Assert.Equal(2, report.Categories[0].TransactionCount);
		Assert.Equal("Other Income", report.Categories[1].Name);
		Assert.Equal(-1000m, report.Categories[1].Amount);
		Assert.Equal(0m, report.Categories[1].PercentOfTotal);
	}

	[Fact]
	public void PercentsRoundToOneDecimal()
	{
		var expenses = new[]
		{
			Expense(1, 1m, 6),
			Expense(2, 2m, 15)
		};
		var categories = new[]
		{
			Cat(6, "Groceries"),
			Cat(15, "Clothing")
		};

		var report = BudgetReportBuilder.Build("2026-01", expenses, NoSplits, categories);

		Assert.Equal("Clothing", report.Categories[0].Name);
		Assert.Equal(66.7m, report.Categories[0].PercentOfTotal);
		Assert.Equal(33.3m, report.Categories[1].PercentOfTotal);
	}

	[Fact]
	public void CategoryThatNetsToZero_IsOmitted()
	{
		var expenses = new[]
		{
			Expense(1, 10m, 6),
			Expense(2, -10m, 6),
			Expense(3, 5m, 15)
		};
		var categories = new[]
		{
			Cat(6, "Groceries"),
			Cat(15, "Clothing")
		};

		var report = BudgetReportBuilder.Build("2026-01", expenses, NoSplits, categories);

		var clothing = Assert.Single(report.Categories);
		Assert.Equal("Clothing", clothing.Name);
		Assert.Equal(5m, report.TotalSpent);
		Assert.Equal(1, report.TransactionCount);
	}

	[Fact]
	public void SameCategory_CountsEachExpense()
	{
		var expenses = new[]
		{
			Expense(1, 10m, 6),
			Expense(2, 5m, 6)
		};

		var groceries = Assert.Single(
			BudgetReportBuilder.Build("2026-01", expenses, NoSplits, new[] { Cat(6, "Groceries") }).Categories);

		Assert.Equal(15m, groceries.Amount);
		Assert.Equal(2, groceries.TransactionCount);
	}

	private static Expense Expense(int id, decimal amount, int? category, bool isSplit = false) =>
		new()
		{
			Expense_I = id,
			Amount = amount,
			Category = category,
			IsSplit = isSplit,
			ExpenseDate = new DateTime(2026, 1, 15)
		};

	private static Category Cat(int id, string name, int? parent = null) =>
		new()
		{
			Category_I = id,
			Name = name,
			ParentCategory_I = parent
		};

	private static ExpenseSplit Split(int expenseId, decimal amount, int category) =>
		new()
		{
			Expense_I = expenseId,
			Amount = amount,
			Category = category
		};
}
