namespace MoneyManager.Data.Models
{
	public class DbExpenseSplit
	{
		public int Id { get; set; }
		public int Expense_I { get; set; }
		public Guid UserId { get; set; }
		public string Description { get; set; } = string.Empty;
		public decimal Amount { get; set; }
		public int Category { get; set; }
		public DateTime CreatedDateTime { get; set; }
	}
}
