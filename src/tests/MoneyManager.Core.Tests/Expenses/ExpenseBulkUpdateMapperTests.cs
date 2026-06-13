using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models.Input;
using Xunit;

namespace MoneyManager.Core.Tests.Expenses;

public class ExpenseBulkUpdateMapperTests
{
	private readonly ExpenseBulkUpdateMapper _mapper = new();

	[Fact]
	public void ToUpdates_MapsAllProvidedFields()
	{
		var request = new BulkUpdateRequest
		{
			ExpenseDate = new DateTime(2024, 3, 1),
			Category = 5,
			DatePaid = new DateTime(2024, 3, 2)
		};

		var updates = _mapper.ToUpdates(request);

		Assert.Equal(request.ExpenseDate, updates[ExpenseFieldNames.ExpenseDate]);
		Assert.Equal(5, updates[ExpenseFieldNames.Category]);
		Assert.Equal(request.DatePaid, updates[ExpenseFieldNames.DatePaid]);
	}

	[Fact]
	public void ToUpdates_MapsNullSentinels()
	{
		var request = new BulkUpdateRequest
		{
			SetCategoryToNull = true,
			SetDatePaidToNull = true
		};

		var updates = _mapper.ToUpdates(request);

		Assert.True(updates.ContainsKey(ExpenseFieldNames.Category));
		Assert.Null(updates[ExpenseFieldNames.Category]);
		Assert.True(updates.ContainsKey(ExpenseFieldNames.DatePaid));
		Assert.Null(updates[ExpenseFieldNames.DatePaid]);
	}

	[Fact]
	public void ToUpdates_ReturnsEmptyWhenNoFieldsSet()
	{
		var updates = _mapper.ToUpdates(new BulkUpdateRequest());

		Assert.Empty(updates);
	}
}
