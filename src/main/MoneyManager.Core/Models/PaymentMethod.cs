using System.Text.Json.Serialization;

namespace MoneyManager.Core.Models
{
	public class PaymentMethod
	{
		public int ID { get; set; }
		[JsonPropertyName("PaymentMethod")]
		public string PaymentMethodName { get; set; } = string.Empty;
	}
}
