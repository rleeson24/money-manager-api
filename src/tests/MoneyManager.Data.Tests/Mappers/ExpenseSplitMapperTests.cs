using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Tests.Helpers;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Data.Tests.Mappers;

public class ExpenseSplitMapperTests : TestBase<ExpenseSplitMapper>
{
	protected static readonly DateTime CreatedDateTime = new(2026, 3, 1, 14, 30, 0, DateTimeKind.Utc);
	protected static readonly Guid UserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

	protected DbExpenseSplit _result = null!;

	public class FromDbReader : ExpenseSplitMapperTests
	{
		public FromDbReader()
		{
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var reader = DictionaryDbDataReader.Create(new Dictionary<string, object?>
			{
				["Id"] = 7,
				["Expense_I"] = 2,
				["UserId"] = UserId,
				["Description"] = "Food portion",
				["Amount"] = 25.75m,
				["Category"] = 19,
				["CreatedDateTime"] = CreatedDateTime
			});
			_result = SubjectUnderTest.FromDbReader(reader).GetAwaiter().GetResult();
		}

		[Fact]
		public void MapsId() => Assert.Equal(7, _result.Id);

		[Fact]
		public void MapsExpenseId() => Assert.Equal(2, _result.Expense_I);

		[Fact]
		public void MapsUserId() => Assert.Equal(UserId, _result.UserId);

		[Fact]
		public void MapsDescription() => Assert.Equal("Food portion", _result.Description);

		[Fact]
		public void MapsAmount() => Assert.Equal(25.75m, _result.Amount);

		[Fact]
		public void MapsCategory() => Assert.Equal(19, _result.Category);

		[Fact]
		public void MapsCreatedDateTime() => Assert.Equal(CreatedDateTime, _result.CreatedDateTime);
	}
}
