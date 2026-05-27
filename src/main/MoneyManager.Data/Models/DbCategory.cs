namespace MoneyManager.Data.Models
{
	public class DbCategory
	{
		public int Category_I { get; set; }
		public string Name { get; set; } = string.Empty;
		public int? ParentCategory_I { get; set; }
		public bool Required { get; set; }
		public bool Archived { get; set; }
	}
}
