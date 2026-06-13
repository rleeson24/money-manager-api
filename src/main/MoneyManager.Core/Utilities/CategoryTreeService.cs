using MoneyManager.Core.Models;

namespace MoneyManager.Core.Utilities
{
	public class CategoryTreeService : ICategoryTreeService
	{
		public IReadOnlyList<Category> WithHasChildren(IReadOnlyList<Category> categories)
		{
			var parentIds = categories
				.Where(c => c.ParentCategory_I.HasValue)
				.Select(c => c.ParentCategory_I!.Value)
				.ToHashSet();

			return categories.Select(c =>
			{
				c.HasChildren = parentIds.Contains(c.Category_I);
				return c;
			}).ToList();
		}

		public IReadOnlyList<Category> SortForDisplay(IReadOnlyList<Category> categories)
		{
			var byId = categories.ToDictionary(c => c.Category_I);
			string SortKey(Category c)
			{
				if (c.ParentCategory_I.HasValue && byId.TryGetValue(c.ParentCategory_I.Value, out var parent))
					return $"{parent.Name}\0{c.Name}";
				return $"{c.Name}\0";
			}

			return categories.OrderBy(SortKey, StringComparer.OrdinalIgnoreCase).ToList();
		}

		public IReadOnlyList<Category> PrepareForDisplay(IReadOnlyList<Category> categories) =>
			WithHasChildren(SortForDisplay(categories));
	}
}
