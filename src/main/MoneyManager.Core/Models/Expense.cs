using System.Text.Json.Serialization;

namespace MoneyManager.Core.Models
{
	public class Expense
	{
		public int Expense_I { get; set; }
		public DateTime ExpenseDate { get; set; }
		[JsonPropertyName("expense")]
		public string ExpenseDescription { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public int? PaymentMethod { get; set; }
		public int? Category { get; set; }
		public DateTime? DatePaid { get; set; }
		public DateTime CreatedDateTime { get; set; }
		public DateTime ModifiedDateTime { get; set; }
	}
}
