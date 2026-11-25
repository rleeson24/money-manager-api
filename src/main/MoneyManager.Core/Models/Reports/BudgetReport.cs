namespace MoneyManager.Core.Models.Reports
{
	public class BudgetReport
	{
		public string Month { get; set; } = string.Empty;
		/// <summary>Sum of category groups that spent money. Income and net credits are excluded.</summary>
		public decimal TotalSpent { get; set; }
		public int TransactionCount { get; set; }
		public IReadOnlyList<BudgetReportCategory> Categories { get; set; } = Array.Empty<BudgetReportCategory>();
	}

	public class BudgetReportCategory
	{
		public int? CategoryId { get; set; }
		public string Name { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		/// <summary>Share of <see cref="BudgetReport.TotalSpent"/>. Zero when this group is not spending.</summary>
		public decimal PercentOfTotal { get; set; }
		public int TransactionCount { get; set; }
		/// <summary>
		/// Child categories, plus spending assigned directly to the parent when the parent also has children.
		/// Empty when this row is a single category.
		/// </summary>
		public IReadOnlyList<BudgetReportLine> Lines { get; set; } = Array.Empty<BudgetReportLine>();
	}

	public class BudgetReportLine
	{
		public int? CategoryId { get; set; }
		public string Name { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public decimal PercentOfTotal { get; set; }
		public int TransactionCount { get; set; }
		/// <summary>True when the amount was categorized on the parent itself rather than a child category.</summary>
		public bool IsDirect { get; set; }
	}
}
