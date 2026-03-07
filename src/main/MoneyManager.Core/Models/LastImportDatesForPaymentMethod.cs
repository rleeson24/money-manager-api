namespace MoneyManager.Core.Models
{
	public class LastImportDatesForPaymentMethod
	{
		public int PaymentMethodId { get; set; }
		public DateTime? LatestExpenseDate { get; set; }
		public DateTime? LatestDatePaid { get; set; }
	}
}
