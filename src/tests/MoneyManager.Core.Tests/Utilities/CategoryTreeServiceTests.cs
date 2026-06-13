using MoneyManager.Core.Models;
using MoneyManager.Core.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Utilities;

public class CategoryTreeServiceTests
{
	private readonly CategoryTreeService _service = new();

	[Fact]
	public void WithHasChildren_SetsFlagOnParentsOnly()
	{
		var categories = new List<Category>
		{
			new() { Category_I = 1, Name = "Food" },
			new() { Category_I = 2, Name = "Groceries", ParentCategory_I = 1 },
			new() { Category_I = 3, Name = "Transport" }
		};

		var result = _service.WithHasChildren(categories);

		Assert.True(result.Single(c => c.Category_I == 1).HasChildren);
		Assert.False(result.Single(c => c.Category_I == 2).HasChildren);
		Assert.False(result.Single(c => c.Category_I == 3).HasChildren);
	}

	[Fact]
	public void SortForDisplay_OrdersParentBeforeChild()
	{
		var categories = new List<Category>
		{
			new() { Category_I = 2, Name = "Zebra", ParentCategory_I = 1 },
			new() { Category_I = 1, Name = "Animals" },
			new() { Category_I = 3, Name = "Apple" }
		};

		var sorted = _service.SortForDisplay(categories);

		Assert.Equal(new[] { 1, 2, 3 }, sorted.Select(c => c.Category_I).ToArray());
	}

	[Fact]
	public void SortForDisplay_IsCaseInsensitive()
	{
		var categories = new List<Category>
		{
			new() { Category_I = 1, Name = "beta" },
			new() { Category_I = 2, Name = "Alpha" }
		};

		var sorted = _service.SortForDisplay(categories);

		Assert.Equal(2, sorted[0].Category_I);
		Assert.Equal(1, sorted[1].Category_I);
	}

	[Fact]
	public void PrepareForDisplay_SortsAndSetsHasChildren()
	{
		var categories = new List<Category>
		{
			new() { Category_I = 2, Name = "Zebra", ParentCategory_I = 1 },
			new() { Category_I = 1, Name = "Animals" }
		};

		var result = _service.PrepareForDisplay(categories);

		Assert.Equal(new[] { 1, 2 }, result.Select(c => c.Category_I).ToArray());
		Assert.True(result.Single(c => c.Category_I == 1).HasChildren);
	}
}
