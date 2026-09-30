using MoneyManager.Core.Expenses;
using Xunit;

namespace MoneyManager.Core.Tests.Expenses;

public class ExpenseFieldNamesTests
{
	[Fact]
	public void AllBulkUpdate_IncludesExcludeFromCredit()
	{
		var expected = new[]
		{
			ExpenseFieldNames.ExpenseDate,
			ExpenseFieldNames.Category,
			ExpenseFieldNames.DatePaid,
			ExpenseFieldNames.ExcludeFromCredit
		};

		Assert.Equal(expected.OrderBy(field => field), ExpenseFieldNames.AllBulkUpdate.OrderBy(field => field));
	}

	[Theory]
	[InlineData(ExpenseFieldNames.ExcludeFromCredit, true)]
	[InlineData(ExpenseFieldNames.Expense, false)]
	[InlineData(ExpenseFieldNames.IsSplit, false)]
	public void IsBulkUpdateField_MatchesBulkUpdateFields(string field, bool expected) =>
		Assert.Equal(expected, ExpenseFieldNames.IsBulkUpdateField(field));
}
