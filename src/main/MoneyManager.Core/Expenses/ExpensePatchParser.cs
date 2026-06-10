using System.Globalization;
using System.Text.Json;

namespace MoneyManager.Core.Expenses
{
	public sealed record ExpensePatchParseResult(
		Dictionary<string, object?> Updates,
		DateTime? ExpectedModifiedDateTime);

	public static class ExpensePatchParser
	{
		public static ExpensePatchParseResult Parse(JsonElement jsonElement)
		{
			var updates = new Dictionary<string, object?>();
			DateTime? expectedModifiedDateTime = null;

			foreach (var prop in jsonElement.EnumerateObject())
			{
				var key = prop.Name;
				var value = prop.Value;

				if (key == ExpenseFieldNames.ModifiedDateTime)
				{
					expectedModifiedDateTime = ParseDateTimeFromElement(value);
					continue;
				}
				if (key == ExpenseFieldNames.CreatedDateTime)
					continue;

				var normalizedKey = ExpenseFieldNames.NormalizeJsonKey(key);

				if (value.ValueKind == JsonValueKind.Null)
				{
					updates[normalizedKey] = null;
				}
				else if (value.ValueKind == JsonValueKind.String)
				{
					var strValue = value.GetString();
					if (normalizedKey is ExpenseFieldNames.ExpenseDate or ExpenseFieldNames.DatePaid)
					{
						if (DateTime.TryParse(strValue, out var dateValue))
							updates[normalizedKey] = dateValue;
					}
					else
					{
						updates[normalizedKey] = strValue;
					}
				}
				else if (value.ValueKind == JsonValueKind.Number)
				{
					if (normalizedKey is ExpenseFieldNames.Amount or ExpenseFieldNames.PaymentMethod or ExpenseFieldNames.Category)
					{
						if (value.TryGetInt32(out var intValue))
							updates[normalizedKey] = intValue;
						else if (value.TryGetDecimal(out var decimalValue))
							updates[normalizedKey] = decimalValue;
					}
				}
				else if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
				{
					updates[normalizedKey] = value.GetBoolean();
				}
			}

			return new ExpensePatchParseResult(updates, expectedModifiedDateTime);
		}

		private static DateTime? ParseDateTimeFromElement(JsonElement value)
		{
			if (value.ValueKind == JsonValueKind.Null) return null;
			if (value.ValueKind == JsonValueKind.String && value.GetString() is { } s)
			{
				return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var d)
					? d
					: null;
			}
			return null;
		}
	}
}
