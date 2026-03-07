using System;
using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Models;

namespace MoneyManager.Data.Repositories
{
	/// <summary>
	/// In-memory mutable store for development. Seeded from MockData; all mutations are process-local.
	/// Thread-safe for concurrent requests.
	/// </summary>
	public class InMemoryStore
	{
		private readonly object _lock = new();
		private readonly List<Expense> _expenses;
		private readonly List<ExpenseSplit> _expenseSplits;
		private int _nextExpenseId;
		private int _nextSplitId;

		public InMemoryStore()
		{
			_expenses = new List<Expense>(MockData.Expenses);
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

		public IReadOnlyList<Category> Categories => MockData.Categories;
		public IReadOnlyList<PaymentMethod> PaymentMethods => MockData.PaymentMethods;

		public IReadOnlyList<Expense> GetExpenses()
		{
			lock (_lock)
				return _expenses.ToList();
		}

		public Expense? GetExpenseById(int id)
		{
			lock (_lock)
				return _expenses.Find(e => e.Expense_I == id);
		}

		public List<Expense> GetExpensesFiltered(string? month, int? paymentMethod, bool? datePaidNull)
		{
			lock (_lock)
			{
				var list = _expenses.AsEnumerable();
				if (!string.IsNullOrEmpty(month))
					list = list.Where(e => e.ExpenseDate.ToString("yyyy-MM") == month);
				if (paymentMethod.HasValue)
					list = list.Where(e => e.PaymentMethod == paymentMethod.Value);
				if (datePaidNull == true)
					list = list.Where(e => e.DatePaid == null);
				return list.OrderByDescending(e => e.ExpenseDate).ToList();
			}
		}

		public Expense AddExpense(Expense expense)
		{
			lock (_lock)
			{
				int id;
				if (expense.Expense_I > 0)
				{
					id = expense.Expense_I;
					_expenses.Add(expense);
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
					CreatedBy = expense.CreatedBy
				};
				_expenses.Add(added);
				return added;
			}
		}

		public bool UpdateExpense(int id, Expense expense)
		{
			lock (_lock)
			{
				var idx = _expenses.FindIndex(e => e.Expense_I == id);
				if (idx < 0) return false;
				_expenses[idx] = expense;
				return true;
			}
		}

		public bool RemoveExpense(int id)
		{
			lock (_lock)
			{
				var idx = _expenses.FindIndex(e => e.Expense_I == id);
				if (idx < 0) return false;
				_expenses.RemoveAt(idx);
				_expenseSplits.RemoveAll(s => s.Expense_I == id);
				return true;
			}
		}

		public int RemoveExpenses(IEnumerable<int> ids)
		{
			var idSet = ids.ToHashSet();
			lock (_lock)
			{
				var removed = _expenses.RemoveAll(e => idSet.Contains(e.Expense_I));
				_expenseSplits.RemoveAll(s => idSet.Contains(s.Expense_I));
				return removed;
			}
		}

		public int UpdateExpenses(IEnumerable<int> ids, Action<Expense> update)
		{
			lock (_lock)
			{
				var count = 0;
				foreach (var e in _expenses.Where(e => ids.Contains(e.Expense_I)))
				{
					update(e);
					count++;
				}
				return count;
			}
		}

		public IReadOnlyList<ExpenseSplit> GetSplitsByExpenseId(int expense_I)
		{
			lock (_lock)
				return _expenseSplits.Where(s => s.Expense_I == expense_I).OrderBy(s => s.Id).ToList();
		}

		public ExpenseSplit? GetSplitById(int id)
		{
			lock (_lock)
				return _expenseSplits.FirstOrDefault(s => s.Id == id);
		}

		public ExpenseSplit AddSplit(ExpenseSplit split)
		{
			lock (_lock)
			{
				var id = split.Id > 0 ? split.Id : _nextSplitId++;
				var s = new ExpenseSplit
				{
					Id = id,
					Expense_I = split.Expense_I,
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

		public bool UpdateSplit(int id, ExpenseSplit split)
		{
			lock (_lock)
			{
				var idx = _expenseSplits.FindIndex(s => s.Id == id);
				if (idx < 0) return false;
				_expenseSplits[idx] = split;
				return true;
			}
		}

		public bool RemoveSplit(int id)
		{
			lock (_lock)
				return _expenseSplits.RemoveAll(s => s.Id == id) > 0;
		}

		public void RemoveSplitsByExpenseId(int expense_I)
		{
			lock (_lock)
				_expenseSplits.RemoveAll(s => s.Expense_I == expense_I);
		}
	}
}
