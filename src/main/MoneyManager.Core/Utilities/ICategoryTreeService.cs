using MoneyManager.Core.Models;

namespace MoneyManager.Core.Utilities
{
	public interface ICategoryTreeService
	{
		IReadOnlyList<Category> WithHasChildren(IReadOnlyList<Category> categories);

		IReadOnlyList<Category> SortForDisplay(IReadOnlyList<Category> categories);

		IReadOnlyList<Category> PrepareForDisplay(IReadOnlyList<Category> categories);
	}
}
