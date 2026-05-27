namespace MoneyManager.Core.Models.Input
{
	public class CreateCategoryModel
	{
		public string Name { get; set; } = string.Empty;
		public int? ParentCategory_I { get; set; }
		public bool Required { get; set; }
	}

	public class UpdateCategoryModel
	{
		public string? Name { get; set; }
		public int? ParentCategory_I { get; set; }
		public bool? Required { get; set; }
		public bool? Archived { get; set; }
		/// <summary>When true, ParentCategory_I is set to null (make top-level).</summary>
		public bool? ClearParent { get; set; }
	}
}
