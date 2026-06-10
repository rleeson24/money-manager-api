namespace MoneyManager.Core.Expenses
{
	public static class ExpenseFieldNames
	{
		public const string ExpenseDate = "ExpenseDate";
		public const string Expense = "Expense";
		public const string Amount = "Amount";
		public const string Currency = "Currency";
		public const string PaymentMethod = "PaymentMethod";
		public const string Category = "Category";
		public const string DatePaid = "DatePaid";
		public const string IsSplit = "IsSplit";
		public const string ExcludeFromCredit = "ExcludeFromCredit";
		public const string ModifiedDateTime = "ModifiedDateTime";
		public const string CreatedDateTime = "CreatedDateTime";

		public const string JsonIsSplit = "isSplit";
		public const string JsonExcludeFromCredit = "excludeFromCredit";

		private static readonly HashSet<string> PatchableFields =
		[
			ExpenseDate,
			Expense,
			Amount,
			Currency,
			PaymentMethod,
			Category,
			DatePaid,
			IsSplit,
			ExcludeFromCredit
		];

		private static readonly HashSet<string> BulkUpdateFields =
		[
			ExpenseDate,
			Category,
			DatePaid
		];

		public static IReadOnlySet<string> AllPatchable => PatchableFields;
		public static IReadOnlySet<string> AllBulkUpdate => BulkUpdateFields;

		public static bool IsPatchableField(string field) => PatchableFields.Contains(field);

		public static bool IsBulkUpdateField(string field) => BulkUpdateFields.Contains(field);

		public static string NormalizeJsonKey(string jsonKey) => jsonKey switch
		{
			JsonIsSplit => IsSplit,
			JsonExcludeFromCredit => ExcludeFromCredit,
			_ => jsonKey
		};
	}
}
