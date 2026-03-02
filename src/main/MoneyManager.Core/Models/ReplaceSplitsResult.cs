using System.Collections.Generic;
using MoneyManager.Core.Models;

namespace MoneyManager.Core.Models
{
	public class ReplaceSplitsResult
	{
		public bool IsSuccess { get; init; }
		public string? ValidationError { get; init; }
		public IReadOnlyList<ExpenseSplit>? Splits { get; init; }

		public static ReplaceSplitsResult Success(IReadOnlyList<ExpenseSplit> splits) =>
			new ReplaceSplitsResult { IsSuccess = true, Splits = splits };

		public static ReplaceSplitsResult Failure(string message) =>
			new ReplaceSplitsResult { IsSuccess = false, ValidationError = message };
	}
}
