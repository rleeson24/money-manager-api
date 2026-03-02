using System.ComponentModel.DataAnnotations;

namespace MoneyManager.Core.Models.Input
{
	public class CreateOrUpdateExpenseSplitModel
	{
		[Required]
		public int Expense_I { get; set; }

		[Required]
		[MaxLength(500)]
		public string Description { get; set; } = string.Empty;

		[Required]
		[Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
		public decimal Amount { get; set; }

		[Required]
		public int Category { get; set; }
	}
}
