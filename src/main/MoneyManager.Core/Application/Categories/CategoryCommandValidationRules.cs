using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Utilities;

namespace MoneyManager.Core.Application.Categories
{
	internal static class CategoryCommandValidationRules
	{
		public static string? ValidateCreate(CreateCategoryModel model, IReadOnlyList<Category> existing)
		{
			if (string.IsNullOrWhiteSpace(model.Name))
				return "Name is required.";
			if (model.Name.Length > 100)
				return "Name must be 100 characters or less.";

			return ValidateParentAssignment(null, model.ParentCategory_I, existing);
		}

		public static string? ValidateUpdate(
			Category current,
			UpdateCategoryModel model,
			IReadOnlyList<Category> existing)
		{
			if (model.Name != null)
			{
				if (string.IsNullOrWhiteSpace(model.Name))
					return "Name is required.";
				if (model.Name.Length > 100)
					return "Name must be 100 characters or less.";
			}

			if (model.Archived == true && current.Category_I == CategoryConstants.SplitCategoryId)
				return "The Split category cannot be archived.";

			int? newParent = current.ParentCategory_I;
			if (model.ClearParent == true)
				newParent = null;
			else if (model.ParentCategory_I.HasValue)
				newParent = model.ParentCategory_I;

			if (model.ClearParent == true || model.ParentCategory_I.HasValue)
			{
				var err = ValidateParentAssignment(current.Category_I, newParent, existing);
				if (err != null)
					return err;
			}

			if (model.ParentCategory_I.HasValue && current.HasChildren)
				return "Cannot assign a parent to a category that has children.";

			return null;
		}

		public static string? ValidateDelete(Category current, IReadOnlyList<Category> existing, bool inUse)
		{
			if (current.Category_I == CategoryConstants.SplitCategoryId)
				return "The Split category cannot be deleted.";

			if (existing.Any(c => c.ParentCategory_I == current.Category_I))
				return "Cannot delete a category that has children. Archive it or reassign children first.";

			if (inUse)
				return "Cannot delete a category that is used by expenses. Archive it instead.";

			return null;
		}

		private static string? ValidateParentAssignment(int? categoryId, int? parentId, IReadOnlyList<Category> existing)
		{
			if (!parentId.HasValue)
				return null;

			if (categoryId.HasValue && parentId.Value == categoryId.Value)
				return "A category cannot be its own parent.";

			var parent = existing.FirstOrDefault(c => c.Category_I == parentId.Value);
			if (parent == null)
				return $"Parent category {parentId.Value} not found.";

			if (parent.ParentCategory_I.HasValue)
				return "Parent must be a top-level category (one level only).";

			if (parent.Archived)
				return "Cannot assign to an archived parent category.";

			return null;
		}
	}
}
