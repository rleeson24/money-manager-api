using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MoneyManager.Core.Models
{
	public class ExpenseSplit
	{
		public int Id { get; set; }
		public int Expense_I { get; set; }
		[JsonPropertyName("description")]
		public string Description { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public int Category { get; set; }
		public DateTime CreatedDateTime { get; set; }
	}
}
