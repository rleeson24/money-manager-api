using MoneyManager.Core.Models;

namespace MoneyManager.Core.Repositories
{
	public class CategoryMutationResult
	{
		public Category? Category { get; init; }
		public string? ValidationError { get; init; }
		public bool IsNotFound => Category == null && ValidationError == null;

		public static CategoryMutationResult Success(Category category) => new() { Category = category };
		public static CategoryMutationResult Error(string message) => new() { ValidationError = message };
		public static CategoryMutationResult NotFound() => new();
	}

	public class CategoryDeleteResult
	{
		public bool Success { get; init; }
		public string? ValidationError { get; init; }
		public bool IsNotFound { get; init; }

		public static CategoryDeleteResult Ok() => new() { Success = true };
		public static CategoryDeleteResult Error(string message) => new() { ValidationError = message };
		public static CategoryDeleteResult NotFound() => new() { IsNotFound = true };
	}
}
