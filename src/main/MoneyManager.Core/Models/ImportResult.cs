namespace MoneyManager.Core.Models
{
	public class ImportResult
	{
		public int Created { get; set; }
		public int SkippedDuplicates { get; set; }
		public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
	}
}
