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
		public string Currency { get; set; } = Constants.CurrencyConstants.Default;
		public int? PaymentMethod { get; set; }
		public int? Category { get; set; }
		public DateTime? DatePaid { get; set; }
		public DateTime CreatedDateTime { get; set; }
		public DateTime ModifiedDateTime { get; set; }
		public bool IsSplit { get; set; }
		public bool ExcludeFromCredit { get; set; }
		/// <summary>User ID of the creator. Always set to the current user on create.</summary>
		public string CreatedBy { get; set; } = string.Empty;
	}
}
