using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Reports;

namespace MoneyManager.Core.Reports
{
	/// <summary>
	/// Rolls a month of expenses into parent categories for budgeting.
	/// A split expense contributes each split line instead of the parent amount,
	/// so a purchase filed under Split is reported under the real categories.
	/// Refunds reduce the category they belong to. The spending total ignores
	/// categories that net to a credit, such as income.
	/// </summary>
	public static class BudgetReportBuilder
	{
		public const string UncategorizedName = "Uncategorized";

		public static BudgetReport Build(
			string month,
			IReadOnlyList<Expense> expenses,
			IReadOnlyList<ExpenseSplit> splits,
			IReadOnlyList<Category> categories)
		{
			var categoryById = categories.ToDictionary(c => c.Category_I);
			var splitsByExpense = splits
				.GroupBy(s => s.Expense_I)
				.ToDictionary(g => g.Key, g => (IReadOnlyList<ExpenseSplit>)g.ToList());

			var buckets = new Dictionary<int, SpendBucket>();
			SpendBucket? uncategorizedBucket = null;

			void Add(int? categoryId, decimal amount)
			{
				SpendBucket bucket;
				if (categoryId == null)
				{
					uncategorizedBucket ??= new SpendBucket();
					bucket = uncategorizedBucket;
				}
				else if (!buckets.TryGetValue(categoryId.Value, out bucket!))
				{
					bucket = new SpendBucket();
					buckets[categoryId.Value] = bucket;
				}

				bucket.Amount += amount;
				bucket.Count += 1;
			}

			foreach (var expense in expenses)
			{
				if (expense.IsSplit
					&& splitsByExpense.TryGetValue(expense.Expense_I, out var expenseSplits)
					&& expenseSplits.Count > 0)
				{
					foreach (var split in expenseSplits)
						Add(split.Category, split.Amount);
				}
				else
				{
					Add(expense.Category, expense.Amount);
				}
			}

			var groups = new Dictionary<int, GroupBuilder>();
			BudgetReportCategory? uncategorized = null;
			var unknowns = new List<BudgetReportCategory>();

			if (uncategorizedBucket != null && uncategorizedBucket.Amount != 0m)
				uncategorized = Leaf(null, UncategorizedName, uncategorizedBucket);

			foreach (var (categoryId, bucket) in buckets)
			{
				if (bucket.Amount == 0m)
					continue;

				if (!categoryById.TryGetValue(categoryId, out var category))
				{
					unknowns.Add(Leaf(categoryId, UnknownName(categoryId), bucket));
					continue;
				}

				if (category.ParentCategory_I is int parentId
					&& categoryById.TryGetValue(parentId, out var parent))
				{
					GroupFor(groups, parent).Lines.Add(Line(category.Category_I, category.Name, bucket, isDirect: false));
					continue;
				}

				GroupFor(groups, category).Direct = bucket;
			}

			var result = new List<BudgetReportCategory>();
			if (uncategorized != null)
				result.Add(uncategorized);
			result.AddRange(unknowns);

			foreach (var group in groups.Values)
			{
				var built = BuildGroup(group);
				if (built != null)
					result.Add(built);
			}

			var totalSpent = result.Where(c => c.Amount > 0m).Sum(c => c.Amount);
			foreach (var category in result)
			{
				category.PercentOfTotal = PercentOfSpending(category.Amount, totalSpent);
				foreach (var line in category.Lines)
					line.PercentOfTotal = PercentOfSpending(line.Amount, totalSpent);
			}

			result.Sort(CompareGroups);

			return new BudgetReport
			{
				Month = month,
				TotalSpent = totalSpent,
				TransactionCount = result.Sum(c => c.TransactionCount),
				Categories = result
			};
		}

		public static string UnknownName(int categoryId) => $"Unknown category ({categoryId})";

		private static GroupBuilder GroupFor(Dictionary<int, GroupBuilder> groups, Category category)
		{
			if (!groups.TryGetValue(category.Category_I, out var group))
			{
				group = new GroupBuilder(category.Category_I, category.Name);
				groups[category.Category_I] = group;
			}

			return group;
		}

		private static BudgetReportCategory? BuildGroup(GroupBuilder group)
		{
			var lines = new List<BudgetReportLine>(group.Lines);
			if (group.Direct != null && lines.Count > 0)
				lines.Add(Line(group.CategoryId, group.Name, group.Direct, isDirect: true));

			lines.Sort(CompareLines);

			decimal amount;
			int count;
			IReadOnlyList<BudgetReportLine> published;
			if (lines.Count == 0)
			{
				if (group.Direct == null || group.Direct.Amount == 0m)
					return null;
				amount = group.Direct.Amount;
				count = group.Direct.Count;
				published = Array.Empty<BudgetReportLine>();
			}
			else
			{
				amount = lines.Sum(l => l.Amount);
				count = lines.Sum(l => l.TransactionCount);
				if (amount == 0m)
					return null;
				published = lines;
			}

			return new BudgetReportCategory
			{
				CategoryId = group.CategoryId,
				Name = group.Name,
				Amount = amount,
				TransactionCount = count,
				Lines = published
			};
		}

		private static BudgetReportCategory Leaf(int? categoryId, string name, SpendBucket bucket) =>
			new()
			{
				CategoryId = categoryId,
				Name = name,
				Amount = bucket.Amount,
				TransactionCount = bucket.Count
			};

		private static BudgetReportLine Line(int? categoryId, string name, SpendBucket bucket, bool isDirect) =>
			new()
			{
				CategoryId = categoryId,
				Name = name,
				Amount = bucket.Amount,
				TransactionCount = bucket.Count,
				IsDirect = isDirect
			};

		private static decimal PercentOfSpending(decimal amount, decimal totalSpent)
		{
			if (totalSpent <= 0m || amount <= 0m)
				return 0m;
			return Math.Round(amount / totalSpent * 100m, 1, MidpointRounding.AwayFromZero);
		}

		private static int CompareGroups(BudgetReportCategory a, BudgetReportCategory b)
		{
			var aSpend = a.Amount > 0m;
			var bSpend = b.Amount > 0m;
			if (aSpend != bSpend)
				return aSpend ? -1 : 1;
			if (aSpend)
				return CompareAmountThenName(b.Amount, b.Name, a.Amount, a.Name);
			return CompareAmountThenName(a.Amount, a.Name, b.Amount, b.Name);
		}

		private static int CompareLines(BudgetReportLine a, BudgetReportLine b) =>
			CompareAmountThenName(b.Amount, b.Name, a.Amount, a.Name);

		private static int CompareAmountThenName(decimal amountA, string nameA, decimal amountB, string nameB)
		{
			var byAmount = amountA.CompareTo(amountB);
			if (byAmount != 0)
				return byAmount;
			return string.Compare(nameA, nameB, StringComparison.OrdinalIgnoreCase);
		}

		private sealed class SpendBucket
		{
			public decimal Amount { get; set; }
			public int Count { get; set; }
		}

		private sealed class GroupBuilder
		{
			public GroupBuilder(int categoryId, string name)
			{
				CategoryId = categoryId;
				Name = name;
			}

			public int CategoryId { get; }
			public string Name { get; }
			public SpendBucket? Direct { get; set; }
			public List<BudgetReportLine> Lines { get; } = new();
		}
	}
}
