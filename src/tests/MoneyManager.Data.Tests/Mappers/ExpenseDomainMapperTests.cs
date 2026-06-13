using Moq;
using MoneyManager.Core;
using MoneyManager.Core.Constants;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Data.Tests.Mappers;

public class ExpenseDomainMapperTests : TestBase<ExpenseDomainMapper>
{
	protected static readonly DateTime FixedUtcNow = new(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
	protected readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

	public ExpenseDomainMapperTests()
	{
		MockFor<INowProvider>().Setup(p => p.UtcNow).Returns(FixedUtcNow);
	}

	public class ToExpense : ExpenseDomainMapperTests
	{
		protected DbExpense _dbExpense = null!;
		protected Expense _result = null!;

		public ToExpense()
		{
			_dbExpense = new DbExpense
			{
				Expense_I = 42,
				ExpenseDate = new DateTime(2026, 3, 15),
				Expense = "Coffee shop",
				Amount = 12.50m,
				Currency = "EUR",
				PaymentMethod = 2,
				Category = 7,
				DatePaid = new DateTime(2026, 3, 20),
				CreatedDate = new DateTime(2026, 3, 15, 8, 0, 0, DateTimeKind.Utc),
				ModifiedDate = new DateTime(2026, 3, 16, 9, 0, 0, DateTimeKind.Utc),
				IsSplit = true,
				ExcludeFromCredit = true,
				CreatedBy = "user-abc"
			};
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod() =>
			_result = SubjectUnderTest.ToExpense(_dbExpense);

		[Fact]
		public void MapsExpenseId() => Assert.Equal(_dbExpense.Expense_I, _result.Expense_I);

		[Fact]
		public void MapsExpenseDate() => Assert.Equal(_dbExpense.ExpenseDate, _result.ExpenseDate);

		[Fact]
		public void MapsDescription() => Assert.Equal(_dbExpense.Expense, _result.ExpenseDescription);

		[Fact]
		public void MapsAmount() => Assert.Equal(_dbExpense.Amount, _result.Amount);

		[Fact]
		public void MapsCurrency() => Assert.Equal(_dbExpense.Currency, _result.Currency);

		[Fact]
		public void MapsPaymentMethod() => Assert.Equal(_dbExpense.PaymentMethod, _result.PaymentMethod);

		[Fact]
		public void MapsCategory() => Assert.Equal(_dbExpense.Category, _result.Category);

		[Fact]
		public void MapsDatePaid() => Assert.Equal(_dbExpense.DatePaid, _result.DatePaid);

		[Fact]
		public void MapsCreatedDateTime() => Assert.Equal(_dbExpense.CreatedDate, _result.CreatedDateTime);

		[Fact]
		public void MapsModifiedDateTime() => Assert.Equal(_dbExpense.ModifiedDate, _result.ModifiedDateTime);

		[Fact]
		public void MapsIsSplit() => Assert.Equal(_dbExpense.IsSplit, _result.IsSplit);

		[Fact]
		public void MapsExcludeFromCredit() => Assert.Equal(_dbExpense.ExcludeFromCredit, _result.ExcludeFromCredit);

		[Fact]
		public void MapsCreatedBy() => Assert.Equal(_dbExpense.CreatedBy, _result.CreatedBy);
	}

	public class ToDbExpense : ExpenseDomainMapperTests
	{
		protected CreateExpenseModel _model = null!;
		protected DbExpense _result = null!;

		public ToDbExpense()
		{
			_model = new CreateExpenseModel
			{
				ExpenseDate = new DateTime(2026, 4, 1),
				Expense = "Groceries",
				Amount = 88.40m,
				Currency = "USD",
				PaymentMethod = 1,
				Category = 6,
				DatePaid = new DateTime(2026, 4, 2),
				IsSplit = false,
				ExcludeFromCredit = false,
				CreatedBy = "creator-id"
			};
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod() =>
			_result = SubjectUnderTest.ToDbExpense(_model, UserId);

		[Fact]
		public void MapsExpenseDate() => Assert.Equal(_model.ExpenseDate, _result.ExpenseDate);

		[Fact]
		public void MapsDescription() => Assert.Equal(_model.Expense, _result.Expense);

		[Fact]
		public void MapsAmount() => Assert.Equal(_model.Amount, _result.Amount);

		[Fact]
		public void MapsCurrency() => Assert.Equal(_model.Currency, _result.Currency);

		[Fact]
		public void MapsPaymentMethod() => Assert.Equal(_model.PaymentMethod, _result.PaymentMethod);

		[Fact]
		public void MapsCategory() => Assert.Equal(_model.Category, _result.Category);

		[Fact]
		public void MapsDatePaid() => Assert.Equal(_model.DatePaid, _result.DatePaid);

		[Fact]
		public void MapsUserId() => Assert.Equal(UserId, _result.UserId);

		[Fact]
		public void SetsCreatedDateFromNowProvider() => Assert.Equal(FixedUtcNow, _result.CreatedDate);

		[Fact]
		public void MapsIsSplit() => Assert.Equal(_model.IsSplit, _result.IsSplit);

		[Fact]
		public void MapsExcludeFromCredit() => Assert.Equal(_model.ExcludeFromCredit, _result.ExcludeFromCredit);

		[Fact]
		public void MapsCreatedBy() => Assert.Equal(_model.CreatedBy, _result.CreatedBy);
	}

	public class ToDbExpense_DefaultCurrency : ExpenseDomainMapperTests
	{
		protected DbExpense _result = null!;

		public ToDbExpense_DefaultCurrency()
		{
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod() =>
			_result = SubjectUnderTest.ToDbExpense(new CreateExpenseModel
			{
				ExpenseDate = new DateTime(2026, 4, 1),
				Expense = "No currency",
				Amount = 10m,
				Currency = "  "
			}, UserId);

		[Fact]
		public void UsesDefaultCurrencyWhenBlank() =>
			Assert.Equal(CurrencyConstants.Default, _result.Currency);
	}

	public class ToDbExpense_DefaultCreatedBy : ExpenseDomainMapperTests
	{
		protected DbExpense _result = null!;

		public ToDbExpense_DefaultCreatedBy()
		{
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod() =>
			_result = SubjectUnderTest.ToDbExpense(new CreateExpenseModel
			{
				ExpenseDate = new DateTime(2026, 4, 1),
				Expense = "No creator",
				Amount = 10m
			}, UserId);

		[Fact]
		public void UsesUserIdWhenCreatedByMissing() =>
			Assert.Equal(UserId.ToString(), _result.CreatedBy);
	}

	public class UpdateFromCreateModel : ExpenseDomainMapperTests
	{
		protected DbExpense _existing = null!;
		protected CreateExpenseModel _model = null!;

		public UpdateFromCreateModel()
		{
			_existing = new DbExpense
			{
				Expense_I = 5,
				ExpenseDate = new DateTime(2026, 1, 1),
				Expense = "Old",
				Amount = 1m,
				Currency = "USD",
				ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};
			_model = new CreateExpenseModel
			{
				ExpenseDate = new DateTime(2026, 5, 5),
				Expense = "Updated",
				Amount = 99.99m,
				Currency = "CAD",
				PaymentMethod = 3,
				Category = 11,
				DatePaid = new DateTime(2026, 5, 6)
			};
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod() =>
			SubjectUnderTest.Update(_existing, _model);

		[Fact]
		public void UpdatesExpenseDate() => Assert.Equal(_model.ExpenseDate, _existing.ExpenseDate);

		[Fact]
		public void UpdatesDescription() => Assert.Equal(_model.Expense, _existing.Expense);

		[Fact]
		public void UpdatesAmount() => Assert.Equal(_model.Amount, _existing.Amount);

		[Fact]
		public void UpdatesCurrency() => Assert.Equal(_model.Currency, _existing.Currency);

		[Fact]
		public void UpdatesPaymentMethod() => Assert.Equal(_model.PaymentMethod, _existing.PaymentMethod);

		[Fact]
		public void UpdatesCategory() => Assert.Equal(_model.Category, _existing.Category);

		[Fact]
		public void UpdatesDatePaid() => Assert.Equal(_model.DatePaid, _existing.DatePaid);

		[Fact]
		public void SetsModifiedDateFromNowProvider() => Assert.Equal(FixedUtcNow, _existing.ModifiedDate);
	}

	public class UpdateFromExpense : ExpenseDomainMapperTests
	{
		protected DbExpense _existing = null!;
		protected Expense _expense = null!;

		public UpdateFromExpense()
		{
			_existing = new DbExpense
			{
				Expense_I = 8,
				ExpenseDate = new DateTime(2026, 1, 1),
				Expense = "Old",
				Amount = 1m,
				Currency = "USD",
				IsSplit = false,
				ExcludeFromCredit = false,
				ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
			};
			_expense = new Expense
			{
				ExpenseDate = new DateTime(2026, 7, 7),
				ExpenseDescription = "Patched expense",
				Amount = 44.44m,
				Currency = "GBP",
				PaymentMethod = 4,
				Category = 22,
				DatePaid = new DateTime(2026, 7, 8),
				IsSplit = true,
				ExcludeFromCredit = true
			};
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod() =>
			SubjectUnderTest.Update(_existing, _expense);

		[Fact]
		public void UpdatesExpenseDate() => Assert.Equal(_expense.ExpenseDate, _existing.ExpenseDate);

		[Fact]
		public void UpdatesDescription() => Assert.Equal(_expense.ExpenseDescription, _existing.Expense);

		[Fact]
		public void UpdatesAmount() => Assert.Equal(_expense.Amount, _existing.Amount);

		[Fact]
		public void UpdatesCurrency() => Assert.Equal(_expense.Currency, _existing.Currency);

		[Fact]
		public void UpdatesPaymentMethod() => Assert.Equal(_expense.PaymentMethod, _existing.PaymentMethod);

		[Fact]
		public void UpdatesCategory() => Assert.Equal(_expense.Category, _existing.Category);

		[Fact]
		public void UpdatesDatePaid() => Assert.Equal(_expense.DatePaid, _existing.DatePaid);

		[Fact]
		public void UpdatesIsSplit() => Assert.Equal(_expense.IsSplit, _existing.IsSplit);

		[Fact]
		public void UpdatesExcludeFromCredit() => Assert.Equal(_expense.ExcludeFromCredit, _existing.ExcludeFromCredit);

		[Fact]
		public void SetsModifiedDateFromNowProvider() => Assert.Equal(FixedUtcNow, _existing.ModifiedDate);
	}
}
