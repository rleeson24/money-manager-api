using System.Text.Json.Serialization;

namespace MoneyManager.Core.Models
{
	public class Expense
	{
		public int Expense_I { get; set; }
		public DateTime ExpenseDate { get; set; }
		[JsonPropertyName("Expense")]
		public string ExpenseDescription { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public int PaymentMethod { get; set; }
		public string Category { get; set; } = string.Empty;
		public DateTime? DatePaid { get; set; }
	}
}
