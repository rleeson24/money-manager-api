using Moq;
using MoneyManager.Core.Application.Reports.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Reports;

public class GetBudgetReportHandlerTests : HandlerBase<GetBudgetReportHandler>
{
	private Mock<IExpenseRepository> _expenses => MockFor<IExpenseRepository>();
	private Mock<IExpenseSplitRepository> _splits => MockFor<IExpenseSplitRepository>();
	private Mock<ICategoryRepository> _categories => MockFor<ICategoryRepository>();

	protected readonly Guid _userId;
	protected readonly CancellationToken _ct;
	protected MoneyManager.Core.Models.Reports.BudgetReport? _result;

	public GetBudgetReportHandlerTests()
	{
		_userId = Fixture.Create<Guid>();
		_ct = CancellationToken.None;
	}

	public class WithSplitExpense_Setup : GetBudgetReportHandlerTests
	{
		public WithSplitExpense_Setup()
		{
			_expenses.Setup(r => r.ListForUser(_userId, "2026-01")).ReturnsAsync(new List<Expense>
			{
				new() { Expense_I = 1, Amount = 10m, Category = 6 },
				new() { Expense_I = 2, Amount = 30m, Category = 19, IsSplit = true }
			});
			_splits.Setup(r => r.ListByExpenseIds(
					_userId,
					It.Is<IReadOnlyList<int>>(ids => ids.Count == 1 && ids[0] == 2)))
				.ReturnsAsync(new List<ExpenseSplit>
				{
					new() { Expense_I = 2, Amount = 20m, Category = 6 },
					new() { Expense_I = 2, Amount = 10m, Category = 21 }
				});
			_categories.Setup(r => r.GetAll(false)).ReturnsAsync(new List<Category>
			{
				new() { Category_I = 6, Name = "Groceries" },
				new() { Category_I = 7, Name = "Entertainment" },
				new() { Category_I = 21, Name = "Dining/Eating Out", ParentCategory_I = 7 },
				new() { Category_I = 19, Name = "Split" }
			});
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetBudgetReportQuery(_userId, "2026-01"), _ct);
		}
	}

	public class WithSplitExpense : IClassFixture<WithSplitExpense_Setup>
	{
		private readonly WithSplitExpense_Setup _fixture;

		public WithSplitExpense(WithSplitExpense_Setup fixture) => _fixture = fixture;

		[Fact]
		public void AttributesSplitLinesToTheirCategories()
		{
			Assert.NotNull(_fixture._result);
			Assert.Equal(40m, _fixture._result!.TotalSpent);
			Assert.Equal("Groceries", _fixture._result.Categories[0].Name);
			Assert.Equal(30m, _fixture._result.Categories[0].Amount);
			Assert.Equal("Entertainment", _fixture._result.Categories[1].Name);
			Assert.DoesNotContain(_fixture._result.Categories, c => c.CategoryId == 19);
		}

		[Fact]
		public void LoadsTheMonthAndOnlySplitExpenses()
		{
			_fixture._expenses.Verify(r => r.ListForUser(_fixture._userId, "2026-01"), Times.Once);
			_fixture._splits.Verify(
				r => r.ListByExpenseIds(_fixture._userId, It.Is<IReadOnlyList<int>>(ids => ids.Count == 1 && ids[0] == 2)),
				Times.Once);
			_fixture._categories.Verify(r => r.GetAll(false), Times.Once);
		}
	}

	public class WithoutSplits_Setup : GetBudgetReportHandlerTests
	{
		public WithoutSplits_Setup()
		{
			_expenses.Setup(r => r.ListForUser(_userId, "2026-02")).ReturnsAsync(new List<Expense>
			{
				new() { Expense_I = 4, Amount = 12m, Category = 6 }
			});
			_categories.Setup(r => r.GetAll(false)).ReturnsAsync(new List<Category>
			{
				new() { Category_I = 6, Name = "Groceries" }
			});
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetBudgetReportQuery(_userId, "2026-02"), _ct);
		}
	}

	public class WithoutSplits : IClassFixture<WithoutSplits_Setup>
	{
		private readonly WithoutSplits_Setup _fixture;

		public WithoutSplits(WithoutSplits_Setup fixture) => _fixture = fixture;

		[Fact]
		public void DoesNotQuerySplits()
		{
			_fixture._splits.Verify(
				r => r.ListByExpenseIds(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<int>>()),
				Times.Never);
			Assert.Equal(12m, _fixture._result!.TotalSpent);
		}
	}
}
