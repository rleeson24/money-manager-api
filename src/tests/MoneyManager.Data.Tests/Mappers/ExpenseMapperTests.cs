using System.Data;
using MoneyManager.Core.Constants;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Tests.Helpers;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Data.Tests.Mappers;

public class ExpenseMapperTests : TestBase<ExpenseMapper>
{
	protected static readonly DateTime ExpenseDate = new(2026, 2, 10);
	protected static readonly DateTime CreatedDate = new(2026, 2, 10, 10, 0, 0, DateTimeKind.Utc);
	protected static readonly DateTime ModifiedDate = new(2026, 2, 11, 11, 0, 0, DateTimeKind.Utc);
	protected static readonly DateTime DatePaid = new(2026, 2, 12);
	protected static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

	protected DbExpense _result = null!;

	public class AllFieldsPopulated : ExpenseMapperTests
	{
		public AllFieldsPopulated()
		{
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var reader = DictionaryDbDataReader.Create(new Dictionary<string, object?>
			{
				["Expense_I"] = 99,
				["ExpenseDate"] = ExpenseDate,
				["Expense"] = "Test purchase",
				["Amount"] = 123.45m,
				["Currency"] = "EUR",
				["PaymentMethod"] = 2,
				["Category"] = 15,
				["DatePaid"] = DatePaid,
				["UserId"] = UserId,
				["CreatedDate"] = CreatedDate,
				["ModifiedDate"] = ModifiedDate,
				["IsSplit"] = true,
				["ExcludeFromCredit"] = true,
				["CreatedBy"] = "import-user"
			});
			_result = SubjectUnderTest.FromDbReader(reader).GetAwaiter().GetResult();
		}

		[Fact]
		public void MapsExpenseId() => Assert.Equal(99, _result.Expense_I);

		[Fact]
		public void MapsExpenseDate() => Assert.Equal(ExpenseDate, _result.ExpenseDate);

		[Fact]
		public void MapsDescription() => Assert.Equal("Test purchase", _result.Expense);

		[Fact]
		public void MapsAmount() => Assert.Equal(123.45m, _result.Amount);

		[Fact]
		public void MapsCurrency() => Assert.Equal("EUR", _result.Currency);

		[Fact]
		public void MapsPaymentMethod() => Assert.Equal(2, _result.PaymentMethod);

		[Fact]
		public void MapsCategory() => Assert.Equal(15, _result.Category);

		[Fact]
		public void MapsDatePaid() => Assert.Equal(DatePaid, _result.DatePaid);

		[Fact]
		public void MapsUserId() => Assert.Equal(UserId, _result.UserId);

		[Fact]
		public void MapsCreatedDate() => Assert.Equal(CreatedDate, _result.CreatedDate);

		[Fact]
		public void MapsModifiedDate() => Assert.Equal(ModifiedDate, _result.ModifiedDate);

		[Fact]
		public void MapsIsSplit() => Assert.True(_result.IsSplit);

		[Fact]
		public void MapsExcludeFromCredit() => Assert.True(_result.ExcludeFromCredit);

		[Fact]
		public void MapsCreatedBy() => Assert.Equal("import-user", _result.CreatedBy);
	}

	public class NullableFieldsNull : ExpenseMapperTests
	{
		public NullableFieldsNull()
		{
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var reader = DictionaryDbDataReader.Create(new Dictionary<string, object?>
			{
				["Expense_I"] = 1,
				["ExpenseDate"] = ExpenseDate,
				["Expense"] = "Nullable fields",
				["Amount"] = 10m,
				["Currency"] = DBNull.Value,
				["PaymentMethod"] = DBNull.Value,
				["Category"] = DBNull.Value,
				["DatePaid"] = DBNull.Value,
				["UserId"] = UserId,
				["CreatedDate"] = CreatedDate,
				["ModifiedDate"] = ModifiedDate,
				["IsSplit"] = false,
				["ExcludeFromCredit"] = false,
				["CreatedBy"] = "seed-user"
			});
			_result = SubjectUnderTest.FromDbReader(reader).GetAwaiter().GetResult();
		}

		[Fact]
		public void UsesDefaultCurrencyWhenNull() =>
			Assert.Equal(CurrencyConstants.Default, _result.Currency);

		[Fact]
		public void MapsNullPaymentMethod() => Assert.Null(_result.PaymentMethod);

		[Fact]
		public void MapsNullCategory() => Assert.Null(_result.Category);

		[Fact]
		public void MapsNullDatePaid() => Assert.Null(_result.DatePaid);
	}
}
