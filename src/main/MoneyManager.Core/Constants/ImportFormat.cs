namespace MoneyManager.Core.Constants
{
	public static class ImportFormat
	{
		public const string Csv = "CSV";

		public static bool IsCsv(string? format) =>
			string.Equals(format?.Trim(), Csv, StringComparison.OrdinalIgnoreCase);
	}
}
