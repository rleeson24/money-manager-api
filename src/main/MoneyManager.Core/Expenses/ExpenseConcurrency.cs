namespace MoneyManager.Core.Expenses
{
	public static class ExpenseConcurrency
	{
		public static bool ModifiedUtcMillisEqual(DateTime fromDbUnspecifiedUtcWallClock, DateTime fromClientUtcOrUnspecified)
		{
			var a = NormalizeModifiedUtcTicks(fromDbUnspecifiedUtcWallClock);
			var b = NormalizeModifiedUtcTicks(fromClientUtcOrUnspecified);
			return a == b;
		}

		public static long NormalizeModifiedUtcTicks(DateTime value)
		{
			var utcWall =
				value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
			return utcWall.Ticks - (utcWall.Ticks % TimeSpan.TicksPerMillisecond);
		}
	}
}
