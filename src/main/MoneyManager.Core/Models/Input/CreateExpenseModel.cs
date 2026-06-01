namespace MoneyManager.Core.Models.Input
{
	public class CreateExpenseModel
	{
		public DateTime ExpenseDate { get; set; }
		public string Expense { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public string Currency { get; set; } = "USD";
		public int? PaymentMethod { get; set; }
		public int? Category { get; set; }
		public DateTime? DatePaid { get; set; }
		public bool IsSplit { get; set; }
        public string? CreatedBy { get; set; }
    }
}
