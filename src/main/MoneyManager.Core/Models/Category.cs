namespace MoneyManager.Core.Models
{
	public class Category
	{
		public int Category_I { get; set; }
		public string Name { get; set; } = string.Empty;
		public int? ParentCategory_I { get; set; }
		public bool Required { get; set; }
		public bool Archived { get; set; }
		/// <summary>Computed when categories are loaded; not persisted.</summary>
		public bool HasChildren { get; set; }
	}
}
