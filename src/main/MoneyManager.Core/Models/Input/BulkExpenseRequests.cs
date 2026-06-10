using System.Text.Json.Serialization;

namespace MoneyManager.Core.Models.Input
{
	public class BulkUpdateRequest
	{
		public List<int> Ids { get; set; } = new();
		public DateTime? ExpenseDate { get; set; }
		public int? Category { get; set; }

		[JsonPropertyName("setCategoryToNull")]
		public bool? SetCategoryToNull { get; set; }

		public DateTime? DatePaid { get; set; }

		[JsonPropertyName("setDatePaidToNull")]
		public bool? SetDatePaidToNull { get; set; }
	}

	public class BulkDeleteRequest
	{
		public List<int> Ids { get; set; } = new();
	}
}
