using System;
using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Constants;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Core.Utilities;
using Microsoft.Extensions.Options;

namespace MoneyManager.Data.Repositories
{
	/// <summary>
	/// In-memory mutable store for development. Seeded from MockData; all mutations are process-local.
	/// Thread-safe for concurrent requests. Expenses are scoped by user id (parity with SQL repositories).
	/// </summary>
	public class InMemoryStore
	{
		private readonly object _lock = new();
		private readonly List<Expense> _expenses;
		private readonly Dictionary<int, Guid> _expenseOwners;
		private readonly List<ExpenseSplit> _expenseSplits;
		private readonly List<Category> _categories;
		private readonly ICategoryTreeService _categoryTreeService;
		private int _nextExpenseId;
		private int _nextSplitId;
		private int _nextCategoryId;

		public InMemoryStore(IOptions<DataOptions> dataOptions, ICategoryTreeService categoryTreeService)
		{
			_categoryTreeService = categoryTreeService;
			var seedUserId = ResolveSeedUserId(dataOptions.Value.AspireSeedUserId);
			_expenses = new List<Expense>(MockData.Expenses);
			_expenseOwners = new Dictionary<int, Guid>();
			foreach (var e in _expenses)
				_expenseOwners[e.Expense_I] = seedUserId;

			_categories = LegacyCategorySeed.Categories.Select(c => new Category
			{
				Category_I = c.Category_I,
				Name = c.Name,
				ParentCategory_I = c.ParentCategory_I,
				Required = c.Required,
				Archived = c.Archived
			}).ToList();
			_nextCategoryId = _categories.Count > 0 ? _categories.Max(c => c.Category_I) + 1 : 1;

			_nextExpenseId = _expenses.Count > 0 ? _expenses.Count + 1 : 1;
			foreach (var e in _expenses)
			{
				if (e.Expense_I >= _nextExpenseId)
					_nextExpenseId = e.Expense_I + 1;
			}
			_expenseSplits = new List<ExpenseSplit>(MockData.ExpenseSplits);
			_nextSplitId = _expenseSplits.Count > 0 ? _expenseSplits.Count + 1 : 1;
			foreach (var s in _expenseSplits)
			{
				if (s.Id >= _nextSplitId)
					_nextSplitId = s.Id + 1;
			}
		}

		private static Guid ResolveSeedUserId(string? configured) =>
			Guid.TryParse(configured, out var parsed) ? parsed : Guid.Parse(AspireConstants.DefaultSeedUserId);

		public IReadOnlyList<Category> GetCategories()
		{
			lock (_lock)
				return FinalizeCategories(_categories);
		}

		public Category? GetCategoryById(int id)
		{
			lock (_lock)
				return FinalizeCategories(_categories).FirstOrDefault(c => c.Category_I == id);
		}

		public CategoryMutationResult CreateCategory(CreateCategoryModel model)
		{
			lock (_lock)
			{
				var id = _nextCategoryId++;
				var cat = new Category
				{
					Category_I = id,
					Name = model.Name.Trim(),
					ParentCategory_I = model.ParentCategory_I,
					Required = model.Required,
					Archived = false
				};
				_categories.Add(cat);
				return CategoryMutationResult.Success(FinalizeCategories(_categories).First(c => c.Category_I == id));
			}
		}

		public CategoryMutationResult UpdateCategory(int id, UpdateCategoryModel model)
		{
			lock (_lock)
			{
				var idx = _categories.FindIndex(c => c.Category_I == id);
				if (idx < 0)
					return CategoryMutationResult.NotFound();

				var cat = _categories[idx];
				if (model.Name != null)
					cat.Name = model.Name.Trim();
				if (model.ClearParent == true)
					cat.ParentCategory_I = null;
				else if (model.ParentCategory_I.HasValue)
					cat.ParentCategory_I = model.ParentCategory_I;
				if (model.Required.HasValue)
					cat.Required = model.Required.Value;
				if (model.Archived.HasValue)
					cat.Archived = model.Archived.Value;

				return CategoryMutationResult.Success(FinalizeCategories(_categories).First(c => c.Category_I == id));
			}
		}

		public CategoryDeleteResult DeleteCategory(int id)
		{
			lock (_lock)
			{
				var idx = _categories.FindIndex(c => c.Category_I == id);
				if (idx < 0)
					return CategoryDeleteResult.NotFound();

				_categories.RemoveAt(idx);
				return CategoryDeleteResult.Ok();
			}
		}

		public bool IsCategoryInUse(int categoryId)
		{
			lock (_lock)
				return _expenses.Any(e => e.Category == categoryId)
					|| _expenseSplits.Any(s => s.Category == categoryId);
		}

		private IReadOnlyList<Category> FinalizeCategories(List<Category> categories) =>
			_categoryTreeService.PrepareForDisplay(categories);

		public IReadOnlyList<PaymentMethod> PaymentMethods => MockData.PaymentMethods;

		public IReadOnlyList<Expense> GetExpensesForUser(Guid userId)
		{
			lock (_lock)
				return _expenses.Where(e => IsOwnedBy(e.Expense_I, userId)).ToList();
		}

		public Expense? GetExpenseById(int id, Guid userId)
		{
			lock (_lock)
			{
				if (!IsOwnedBy(id, userId))
					return null;
				return _expenses.Find(e => e.Expense_I == id);
			}
		}

		public bool ExpenseOwnedBy(int expenseId, Guid userId)
		{
			lock (_lock)
				return IsOwnedBy(expenseId, userId);
		}

		public List<Expense> GetExpensesFiltered(Guid userId, string? month, int? paymentMethod, bool? datePaidNull, string? currency = null)
		{
			lock (_lock)
			{
				var list = _expenses.Where(e => IsOwnedBy(e.Expense_I, userId));
				if (!string.IsNullOrEmpty(month))
					list = list.Where(e => e.ExpenseDate.ToString("yyyy-MM") == month);
				if (paymentMethod.HasValue)
					list = list.Where(e => e.PaymentMethod == paymentMethod.Value);
				if (datePaidNull == true)
					list = list.Where(e => e.DatePaid == null);
				if (!string.IsNullOrWhiteSpace(currency))
					list = list.Where(e => string.Equals(e.Currency ?? CurrencyConstants.Default, currency, StringComparison.OrdinalIgnoreCase));
				return list.OrderBy(e => e.ExpenseDate).ToList();
			}
		}

		public Expense AddExpense(Guid userId, Expense expense)
		{
			lock (_lock)
			{
				int id;
				if (expense.Expense_I > 0)
				{
					id = expense.Expense_I;
					_expenses.Add(expense);
					_expenseOwners[id] = userId;
					return expense;
				}
				id = _nextExpenseId++;
				var added = new Expense
				{
					Expense_I = id,
					ExpenseDate = expense.ExpenseDate,
					ExpenseDescription = expense.ExpenseDescription,
					Amount = expense.Amount,
					PaymentMethod = expense.PaymentMethod,
					Category = expense.Category,
					DatePaid = expense.DatePaid,
					CreatedDateTime = expense.CreatedDateTime,
					ModifiedDateTime = expense.ModifiedDateTime,
					IsSplit = expense.IsSplit,
					ExcludeFromCredit = expense.ExcludeFromCredit,
					CreatedBy = expense.CreatedBy
				};
				_expenses.Add(added);
				_expenseOwners[id] = userId;
				return added;
			}
		}

		public bool UpdateExpense(int id, Guid userId, Expense expense)
		{
			lock (_lock)
			{
				if (!IsOwnedBy(id, userId))
					return false;
				var idx = _expenses.FindIndex(e => e.Expense_I == id);
				if (idx < 0) return false;
				_expenses[idx] = expense;
				return true;
			}
		}

		public bool RemoveExpense(int id, Guid userId)
		{
			lock (_lock)
			{
				if (!IsOwnedBy(id, userId))
					return false;
				var idx = _expenses.FindIndex(e => e.Expense_I == id);
				if (idx < 0) return false;
				_expenses.RemoveAt(idx);
				_expenseOwners.Remove(id);
				_expenseSplits.RemoveAll(s => s.Expense_I == id);
				return true;
			}
		}

		public int RemoveExpenses(IEnumerable<int> ids, Guid userId)
		{
			var idSet = ids.ToHashSet();
			lock (_lock)
			{
				var ownedIds = idSet.Where(id => IsOwnedBy(id, userId)).ToHashSet();
				var removed = _expenses.RemoveAll(e => ownedIds.Contains(e.Expense_I));
				foreach (var id in ownedIds)
					_expenseOwners.Remove(id);
				_expenseSplits.RemoveAll(s => ownedIds.Contains(s.Expense_I));
				return removed;
			}
		}

		public int UpdateExpenses(IEnumerable<int> ids, Guid userId, Action<Expense> update)
		{
			lock (_lock)
			{
				var idSet = ids.ToHashSet();
				var count = 0;
				foreach (var e in _expenses.Where(e => idSet.Contains(e.Expense_I) && IsOwnedBy(e.Expense_I, userId)))
				{
					update(e);
					count++;
				}
				return count;
			}
		}

		public IReadOnlyList<ExpenseSplit> GetSplitsByExpenseId(int expense_I, Guid userId)
		{
			lock (_lock)
			{
				if (!IsOwnedBy(expense_I, userId))
					return Array.Empty<ExpenseSplit>();
				return _expenseSplits.Where(s => s.Expense_I == expense_I).OrderBy(s => s.Id).ToList();
			}
		}

		public ExpenseSplit? GetSplitById(int id, Guid userId)
		{
			lock (_lock)
			{
				var split = _expenseSplits.FirstOrDefault(s => s.Id == id);
				if (split == null || !IsOwnedBy(split.Expense_I, userId))
					return null;
				return split;
			}
		}

		public ExpenseSplit AddSplit(int expense_I, Guid userId, ExpenseSplit split)
		{
			lock (_lock)
			{
				if (!IsOwnedBy(expense_I, userId))
					throw new InvalidOperationException("Cannot add split for expense not owned by user.");
				var id = split.Id > 0 ? split.Id : _nextSplitId++;
				var s = new ExpenseSplit
				{
					Id = id,
					Expense_I = expense_I,
					Description = split.Description,
					Amount = split.Amount,
					Category = split.Category,
					CreatedDateTime = split.CreatedDateTime
				};
				_expenseSplits.Add(s);
				if (id >= _nextSplitId) _nextSplitId = id + 1;
				return s;
			}
		}

		public bool UpdateSplit(int id, Guid userId, ExpenseSplit split)
		{
			lock (_lock)
			{
				var existing = _expenseSplits.FirstOrDefault(s => s.Id == id);
				if (existing == null || !IsOwnedBy(existing.Expense_I, userId))
					return false;
				var idx = _expenseSplits.FindIndex(s => s.Id == id);
				_expenseSplits[idx] = split;
				return true;
			}
		}

		public bool RemoveSplit(int id, Guid userId)
		{
			lock (_lock)
			{
				var existing = _expenseSplits.FirstOrDefault(s => s.Id == id);
				if (existing == null || !IsOwnedBy(existing.Expense_I, userId))
					return false;
				return _expenseSplits.RemoveAll(s => s.Id == id) > 0;
			}
		}

		public bool RemoveSplitsByExpenseId(int expense_I, Guid userId)
		{
			lock (_lock)
			{
				if (!IsOwnedBy(expense_I, userId))
					return false;
				_expenseSplits.RemoveAll(s => s.Expense_I == expense_I);
				return true;
			}
		}

		private bool IsOwnedBy(int expenseId, Guid userId) =>
			_expenseOwners.TryGetValue(expenseId, out var owner) && owner == userId;
	}
}
