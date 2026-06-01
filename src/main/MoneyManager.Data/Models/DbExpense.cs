namespace MoneyManager.Data.Models
{
	public class DbExpense
	{
		public int Expense_I { get; set; }
		public DateTime ExpenseDate { get; set; }
		public string Expense { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public string Currency { get; set; } = "USD";
		public int? PaymentMethod { get; set; }
		public int? Category { get; set; }
		public DateTime? DatePaid { get; set; }
		public Guid UserId { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime ModifiedDate { get; set; }
		public bool IsSplit { get; set; }
		/// <summary>User ID of the creator. Always set on create.</summary>
		public string CreatedBy { get; set; } = string.Empty;
	}
}
