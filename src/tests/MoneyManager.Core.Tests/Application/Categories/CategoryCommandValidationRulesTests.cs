using MoneyManager.Core.Application.Categories;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Application.Categories;

public class CategoryCommandValidationRulesTests
{
	private static Category Cat(int id, string name, int? parent = null, bool archived = false, bool hasChildren = false) =>
		new()
		{
			Category_I = id,
			Name = name,
			ParentCategory_I = parent,
			Archived = archived,
			HasChildren = hasChildren
		};

	[Fact]
	public void ValidateCreate_ReturnsNullForValidTopLevel()
	{
		var existing = new List<Category> { Cat(1, "Food") };
		var model = new CreateCategoryModel { Name = "Groceries" };

		Assert.Null(CategoryCommandValidationRules.ValidateCreate(model, existing));
	}

	[Fact]
	public void ValidateCreate_ReturnsErrorWhenNameEmpty()
	{
		var model = new CreateCategoryModel { Name = "   " };

		Assert.Equal("Name is required.", CategoryCommandValidationRules.ValidateCreate(model, Array.Empty<Category>()));
	}

	[Fact]
	public void ValidateCreate_ReturnsErrorWhenParentNotFound()
	{
		var model = new CreateCategoryModel { Name = "Child", ParentCategory_I = 99 };

		Assert.Equal("Parent category 99 not found.", CategoryCommandValidationRules.ValidateCreate(model, Array.Empty<Category>()));
	}

	[Fact]
	public void ValidateCreate_ReturnsErrorWhenParentIsSubcategory()
	{
		var existing = new List<Category>
		{
			Cat(1, "Food"),
			Cat(2, "Groceries", parent: 1)
		};
		var model = new CreateCategoryModel { Name = "Deep", ParentCategory_I = 2 };

		Assert.Equal(
			"Parent must be a top-level category (one level only).",
			CategoryCommandValidationRules.ValidateCreate(model, existing));
	}

	[Fact]
	public void ValidateUpdate_ReturnsErrorWhenArchivingSplitCategory()
	{
		var existing = new List<Category> { Cat(CategoryConstants.SplitCategoryId, "Split") };
		var current = existing[0];
		var model = new UpdateCategoryModel { Archived = true };

		Assert.Equal(
			"The Split category cannot be archived.",
			CategoryCommandValidationRules.ValidateUpdate(current, model, existing));
	}

	[Fact]
	public void ValidateUpdate_ReturnsErrorWhenAssigningParentToCategoryWithChildren()
	{
		var existing = new List<Category>
		{
			Cat(1, "Food"),
			Cat(2, "Parent", hasChildren: true),
			Cat(3, "Child", parent: 2)
		};
		var current = existing[1];
		var model = new UpdateCategoryModel { ParentCategory_I = 1 };

		Assert.Equal(
			"Cannot assign a parent to a category that has children.",
			CategoryCommandValidationRules.ValidateUpdate(current, model, existing));
	}

	[Fact]
	public void ValidateDelete_ReturnsErrorForSplitCategory()
	{
		var current = Cat(CategoryConstants.SplitCategoryId, "Split");

		Assert.Equal(
			"The Split category cannot be deleted.",
			CategoryCommandValidationRules.ValidateDelete(current, Array.Empty<Category>(), inUse: false));
	}

	[Fact]
	public void ValidateDelete_ReturnsErrorWhenCategoryHasChildren()
	{
		var existing = new List<Category>
		{
			Cat(1, "Food"),
			Cat(2, "Groceries", parent: 1)
		};

		Assert.Equal(
			"Cannot delete a category that has children. Archive it or reassign children first.",
			CategoryCommandValidationRules.ValidateDelete(existing[0], existing, inUse: false));
	}

	[Fact]
	public void ValidateDelete_ReturnsErrorWhenCategoryInUse()
	{
		var current = Cat(5, "Transport");

		Assert.Equal(
			"Cannot delete a category that is used by expenses. Archive it instead.",
			CategoryCommandValidationRules.ValidateDelete(current, Array.Empty<Category>(), inUse: true));
	}

}
