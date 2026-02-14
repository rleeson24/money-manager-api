namespace MoneyManager.Data.Models
{
	public class DbExpense
	{
		public int Expense_I { get; set; }
		public DateTime ExpenseDate { get; set; }
		public string Expense { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public int? PaymentMethod { get; set; }
		public string? Category { get; set; }
		public DateTime? DatePaid { get; set; }
		public Guid UserId { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime? ModifiedDate { get; set; }
	}
}
