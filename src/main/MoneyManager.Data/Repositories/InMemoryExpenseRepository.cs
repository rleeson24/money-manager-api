using System;
using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Core.UseCases.Expenses;

namespace MoneyManager.Data.Repositories
{
	public class InMemoryExpenseRepository : IExpenseRepository
	{
		private readonly InMemoryStore _store;
		private readonly INowProvider _nowProvider;

		public InMemoryExpenseRepository(InMemoryStore store, INowProvider nowProvider)
		{
			_store = store;
			_nowProvider = nowProvider;
		}

		public Task<Expense?> Get(int id, Guid userId) =>
			Task.FromResult(_store.GetExpenseById(id));

		public Task<IReadOnlyList<Expense>> ListForUser(Guid userId, string? month = null)
		{
			var list = _store.GetExpensesFiltered(month, null, null);
			return Task.FromResult<IReadOnlyList<Expense>>(list);
		}

		public Task<IReadOnlyList<Expense>> ListForUserWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null)
		{
			var list = _store.GetExpensesFiltered(null, paymentMethod, datePaidNull);
			return Task.FromResult<IReadOnlyList<Expense>>(list);
		}

		public Task<Expense?> Create(Guid userId, CreateExpenseModel model)
		{
			var now = _nowProvider.UtcNow;
			var expense = new Expense
			{
				Expense_I = 0,
				ExpenseDate = model.ExpenseDate,
				ExpenseDescription = model.Expense,
				Amount = model.Amount,
				Currency = string.IsNullOrWhiteSpace(model.Currency) ? "USD" : model.Currency,
				PaymentMethod = model.PaymentMethod,
				Category = model.Category,
				DatePaid = model.DatePaid,
				CreatedDateTime = now,
				ModifiedDateTime = now,
				IsSplit = model.IsSplit,
				ExcludeFromCredit = model.ExcludeFromCredit,
				CreatedBy = model.CreatedBy ?? userId.ToString()
			};
			var added = _store.AddExpense(expense);
			return Task.FromResult<Expense?>(added);
		}

		public Task<UpdateExpenseResult> Update(int id, Guid userId, Expense expense)
		{
			var existing = _store.GetExpenseById(id);
			if (existing == null) return Task.FromResult(UpdateExpenseResult.NotFound());
			if (existing.ModifiedDateTime != expense.ModifiedDateTime)
				return Task.FromResult(UpdateExpenseResult.Conflict(existing));
			var toSave = new Expense
			{
				Expense_I = id,
				ExpenseDate = expense.ExpenseDate,
				ExpenseDescription = expense.ExpenseDescription,
				Amount = expense.Amount,
				Currency = string.IsNullOrWhiteSpace(expense.Currency) ? "USD" : expense.Currency,
				PaymentMethod = expense.PaymentMethod,
				Category = expense.Category,
				DatePaid = expense.DatePaid,
				CreatedDateTime = existing.CreatedDateTime,
				ModifiedDateTime = expense.ModifiedDateTime,
				IsSplit = expense.IsSplit,
				ExcludeFromCredit = expense.ExcludeFromCredit,
				CreatedBy = existing.CreatedBy
			};
			if (!_store.UpdateExpense(id, toSave))
				return Task.FromResult(UpdateExpenseResult.NotFound());
			return Task.FromResult(UpdateExpenseResult.Success(toSave));
		}

		public Task<UpdateExpenseResult> Patch(int id, Guid userId, Dictionary<string, object?> updates, DateTime? expectedModifiedDateTime)
		{
			var current = _store.GetExpenseById(id);
			if (current == null) return Task.FromResult(UpdateExpenseResult.NotFound());
			if (expectedModifiedDateTime.HasValue && current.ModifiedDateTime != expectedModifiedDateTime.Value)
				return Task.FromResult(UpdateExpenseResult.Conflict(current));
			var patched = new Expense
			{
				Expense_I = current.Expense_I,
				ExpenseDate = updates.TryGetValue("ExpenseDate", out var d) && d is DateTime dt ? dt : current.ExpenseDate,
				ExpenseDescription = updates.TryGetValue("Expense", out var desc) && desc is string s ? s : current.ExpenseDescription,
				Amount = updates.TryGetValue("Amount", out var a) && a is decimal amt ? amt : current.Amount,
				Currency = updates.TryGetValue("Currency", out var cur) && cur is string curStr ? curStr : current.Currency,
				PaymentMethod = updates.ContainsKey("PaymentMethod") ? (int?)updates["PaymentMethod"] : current.PaymentMethod,
				Category = updates.ContainsKey("Category") ? (int?)updates["Category"] : current.Category,
				DatePaid = updates.ContainsKey("DatePaid") ? (DateTime?)updates["DatePaid"] : current.DatePaid,
				CreatedDateTime = current.CreatedDateTime,
				ModifiedDateTime = _nowProvider.UtcNow,
				IsSplit = updates.TryGetValue("IsSplit", out var isSplitObj) && isSplitObj is bool isSplitVal ? isSplitVal : current.IsSplit,
				ExcludeFromCredit = updates.TryGetValue("ExcludeFromCredit", out var excludeObj) && excludeObj is bool excludeVal ? excludeVal : current.ExcludeFromCredit,
				CreatedBy = current.CreatedBy
			};
			_store.UpdateExpense(id, patched);
			return Task.FromResult(UpdateExpenseResult.Success(patched));
		}

		public Task<bool> Delete(int id, Guid userId) =>
			Task.FromResult(_store.RemoveExpense(id));

		public Task<bool> BulkUpdate(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates)
		{
			var idList = ids.ToList();
			if (idList.Count == 0 || updates.Count == 0) return Task.FromResult(false);
			var now = _nowProvider.UtcNow;
			var count = _store.UpdateExpenses(idList, e =>
			{
				if (updates.TryGetValue("ExpenseDate", out var d) && d is DateTime dt) e.ExpenseDate = dt;
				if (updates.TryGetValue("Expense", out var desc) && desc is string s) e.ExpenseDescription = s;
				if (updates.TryGetValue("Amount", out var a) && a is decimal amt) e.Amount = amt;
				if (updates.TryGetValue("Currency", out var cur) && cur is string curStr) e.Currency = curStr;
				if (updates.ContainsKey("PaymentMethod")) e.PaymentMethod = (int?)updates["PaymentMethod"];
				if (updates.ContainsKey("Category")) e.Category = (int?)updates["Category"];
				if (updates.ContainsKey("DatePaid")) e.DatePaid = (DateTime?)updates["DatePaid"];
				if (updates.TryGetValue("IsSplit", out var isSplitObj) && isSplitObj is bool isSplitVal) e.IsSplit = isSplitVal;
				if (updates.TryGetValue("ExcludeFromCredit", out var excludeObj) && excludeObj is bool excludeVal) e.ExcludeFromCredit = excludeVal;
				e.ModifiedDateTime = now;
			});
			return Task.FromResult(count > 0);
		}

		public Task<bool> BulkDelete(IEnumerable<int> ids, Guid userId)
		{
			var idList = ids.ToList();
			if (idList.Count == 0) return Task.FromResult(false);
			var removed = _store.RemoveExpenses(idList);
			return Task.FromResult(removed > 0);
		}

		public Task<IReadOnlyList<Expense>> ListForUserInDateRange(Guid userId, DateTime fromDate, DateTime toDate)
		{
			var all = _store.GetExpensesFiltered(null, null, null);
			var list = all.Where(e => e.ExpenseDate.Date >= fromDate.Date && e.ExpenseDate.Date <= toDate.Date).OrderByDescending(e => e.ExpenseDate).ToList();
			return Task.FromResult<IReadOnlyList<Expense>>(list);
		}

		public Task<IReadOnlyList<LastImportDatesForPaymentMethod>> GetLastImportDates(Guid userId, IReadOnlyList<int> paymentMethodIds)
		{
			var all = _store.GetExpensesFiltered(null, null, null);
			var results = new List<LastImportDatesForPaymentMethod>();
			foreach (var pmId in paymentMethodIds)
			{
				var forPm = all.Where(e => e.PaymentMethod == pmId).ToList();
				results.Add(new LastImportDatesForPaymentMethod
				{
					PaymentMethodId = pmId,
					LatestExpenseDate = forPm.Any() ? forPm.Max(e => e.ExpenseDate) : null,
					LatestDatePaid = forPm.Where(e => e.DatePaid.HasValue).Any() ? forPm.Where(e => e.DatePaid.HasValue).Max(e => e.DatePaid!.Value) : null
				});
			}
			return Task.FromResult<IReadOnlyList<LastImportDatesForPaymentMethod>>(results);
		}
	}
}
