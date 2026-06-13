using System.Text.Json;
using MoneyManager.Core.Expenses;
using Xunit;

namespace MoneyManager.Core.Tests.Expenses;

public class ExpensePatchParserTests
{
	private readonly ExpensePatchParser _parser = new();
	[Fact]
	public void Parse_ParsesStringAmountAndDateFields()
	{
		var json = """
			{
				"Expense": "Lunch",
				"Amount": 12.50,
				"ExpenseDate": "2025-03-01",
				"Currency": "USD"
			}
			""";
		var element = JsonDocument.Parse(json).RootElement;

		var result = _parser.Parse(element);

		Assert.Equal("Lunch", result.Updates[ExpenseFieldNames.Expense]);
		Assert.Equal(12.50m, result.Updates[ExpenseFieldNames.Amount]);
		Assert.Equal(new DateTime(2025, 3, 1), result.Updates[ExpenseFieldNames.ExpenseDate]);
		Assert.Equal("USD", result.Updates[ExpenseFieldNames.Currency]);
		Assert.Null(result.ExpectedModifiedDateTime);
	}

	[Fact]
	public void Parse_NormalizesJsonKeysAndBooleans()
	{
		var json = """
			{
				"isSplit": true,
				"excludeFromCredit": false,
				"PaymentMethod": 3,
				"Category": null
			}
			""";
		var element = JsonDocument.Parse(json).RootElement;

		var result = _parser.Parse(element);

		Assert.True((bool)result.Updates[ExpenseFieldNames.IsSplit]!);
		Assert.False((bool)result.Updates[ExpenseFieldNames.ExcludeFromCredit]!);
		Assert.Equal(3, result.Updates[ExpenseFieldNames.PaymentMethod]);
		Assert.True(result.Updates.ContainsKey(ExpenseFieldNames.Category));
		Assert.Null(result.Updates[ExpenseFieldNames.Category]);
	}

	[Fact]
	public void Parse_ExtractsModifiedDateTimeAndIgnoresCreatedDateTime()
	{
		var json = """
			{
				"ModifiedDateTime": "2025-06-10T14:30:00Z",
				"CreatedDateTime": "2025-01-01T00:00:00Z",
				"Expense": "Test"
			}
			""";
		var element = JsonDocument.Parse(json).RootElement;

		var result = _parser.Parse(element);

		Assert.NotNull(result.ExpectedModifiedDateTime);
		Assert.Equal(new DateTime(2025, 6, 10, 14, 30, 0, DateTimeKind.Utc), result.ExpectedModifiedDateTime);
		Assert.False(result.Updates.ContainsKey(ExpenseFieldNames.CreatedDateTime));
		Assert.Equal("Test", result.Updates[ExpenseFieldNames.Expense]);
	}
}
