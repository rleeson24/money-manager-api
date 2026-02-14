namespace MoneyManager.Core.Models.Input
{
	public class CreateExpenseModel
	{
		public DateTime ExpenseDate { get; set; }
		public string Expense { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public int? PaymentMethod { get; set; }
		public string? Category { get; set; }
		public DateTime? DatePaid { get; set; }
	}
}
