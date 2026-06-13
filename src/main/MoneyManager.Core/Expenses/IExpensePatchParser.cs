using System.Text.Json;

namespace MoneyManager.Core.Expenses
{
	public sealed record ExpensePatchParseResult(
		Dictionary<string, object?> Updates,
		DateTime? ExpectedModifiedDateTime);

	public interface IExpensePatchParser
	{
		ExpensePatchParseResult Parse(JsonElement jsonElement);
	}
}
