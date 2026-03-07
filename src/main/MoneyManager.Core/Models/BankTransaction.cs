namespace MoneyManager.Core.Models
{
	/// <summary>
	/// Parsed bank transaction from OFX/QFX/CSV for import. AccountType drives sign rules.
	/// </summary>
	public class BankTransaction
	{
		public DateTime Date { get; set; }
		public decimal Amount { get; set; }
		public string Description { get; set; } = string.Empty;
		/// <summary>Depository (checking/savings) or CreditCard for sign rules.</summary>
		public BankAccountType AccountType { get; set; }
	}

	public enum BankAccountType
	{
		Depository,
		CreditCard
	}
}
