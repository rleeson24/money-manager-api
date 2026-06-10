using System;
using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core;
using MoneyManager.Core.Constants;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;

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
			Task.FromResult(_store.GetExpenseById(id, userId));

		public Task<IReadOnlyList<Expense>> ListForUser(Guid userId, string? month = null)
		{
			var list = _store.GetExpensesFiltered(userId, month, null, null);
			return Task.FromResult<IReadOnlyList<Expense>>(list);
		}

		public Task<IReadOnlyList<Expense>> ListForUserWithFilters(Guid userId, int? paymentMethod = null, bool? datePaidNull = null, string? currency = null)
		{
			var list = _store.GetExpensesFiltered(userId, null, paymentMethod, datePaidNull, currency);
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
				Currency = string.IsNullOrWhiteSpace(model.Currency) ? CurrencyConstants.Default : model.Currency,
				PaymentMethod = model.PaymentMethod,
				Category = model.Category,
				DatePaid = model.DatePaid,
				CreatedDateTime = now,
				ModifiedDateTime = now,
				IsSplit = model.IsSplit,
				ExcludeFromCredit = model.ExcludeFromCredit,
				CreatedBy = model.CreatedBy ?? userId.ToString()
			};
			var added = _store.AddExpense(userId, expense);
			return Task.FromResult<Expense?>(added);
		}

		public Task<UpdateExpenseResult> Update(int id, Guid userId, Expense expense)
		{
			var existing = _store.GetExpenseById(id, userId);
			if (existing == null) return Task.FromResult(UpdateExpenseResult.NotFound());
			if (!ExpenseConcurrency.ModifiedUtcMillisEqual(existing.ModifiedDateTime, expense.ModifiedDateTime))
				return Task.FromResult(UpdateExpenseResult.Conflict(existing));
			var toSave = new Expense
			{
				Expense_I = id,
				ExpenseDate = expense.ExpenseDate,
				ExpenseDescription = expense.ExpenseDescription,
				Amount = expense.Amount,
				Currency = string.IsNullOrWhiteSpace(expense.Currency) ? CurrencyConstants.Default : expense.Currency,
				PaymentMethod = expense.PaymentMethod,
				Category = expense.Category,
				DatePaid = expense.DatePaid,
				CreatedDateTime = existing.CreatedDateTime,
				ModifiedDateTime = expense.ModifiedDateTime,
				IsSplit = expense.IsSplit,
				ExcludeFromCredit = expense.ExcludeFromCredit,
				CreatedBy = existing.CreatedBy
			};
			if (!_store.UpdateExpense(id, userId, toSave))
				return Task.FromResult(UpdateExpenseResult.NotFound());
			return Task.FromResult(UpdateExpenseResult.Success(toSave));
		}

		public Task<UpdateExpenseResult> Patch(int id, Guid userId, Dictionary<string, object?> updates, DateTime? expectedModifiedDateTime)
		{
			var current = _store.GetExpenseById(id, userId);
			if (current == null) return Task.FromResult(UpdateExpenseResult.NotFound());
			if (expectedModifiedDateTime.HasValue
				&& !ExpenseConcurrency.ModifiedUtcMillisEqual(current.ModifiedDateTime, expectedModifiedDateTime.Value))
				return Task.FromResult(UpdateExpenseResult.Conflict(current));
			var patched = ExpensePatchApplicator.Apply(current, updates, _nowProvider.UtcNow);
			if (!_store.UpdateExpense(id, userId, patched))
				return Task.FromResult(UpdateExpenseResult.NotFound());
			return Task.FromResult(UpdateExpenseResult.Success(patched));
		}

		public Task<bool> Delete(int id, Guid userId) =>
			Task.FromResult(_store.RemoveExpense(id, userId));

		public Task<bool> BulkUpdate(IEnumerable<int> ids, Guid userId, Dictionary<string, object?> updates)
		{
			var idList = ids.ToList();
			if (idList.Count == 0 || updates.Count == 0) return Task.FromResult(false);
			var now = _nowProvider.UtcNow;
			var count = _store.UpdateExpenses(idList, userId, e =>
				ExpensePatchApplicator.ApplyTo(e, updates, now));
			return Task.FromResult(count > 0);
		}

		public Task<bool> BulkDelete(IEnumerable<int> ids, Guid userId)
		{
			var idList = ids.ToList();
			if (idList.Count == 0) return Task.FromResult(false);
			var removed = _store.RemoveExpenses(idList, userId);
			return Task.FromResult(removed > 0);
		}

		public Task<IReadOnlyList<Expense>> ListForUserInDateRange(Guid userId, DateTime fromDate, DateTime toDate, int? paymentMethodId = null)
		{
			var all = _store.GetExpensesFiltered(userId, null, null, null);
			var list = all
				.Where(e => e.ExpenseDate.Date >= fromDate.Date && e.ExpenseDate.Date <= toDate.Date)
				.Where(e => !paymentMethodId.HasValue || e.PaymentMethod == paymentMethodId.Value)
				.OrderByDescending(e => e.ExpenseDate)
				.ToList();
			return Task.FromResult<IReadOnlyList<Expense>>(list);
		}

		public Task<IReadOnlyList<LastImportDatesForPaymentMethod>> GetLastImportDates(Guid userId, IReadOnlyList<int> paymentMethodIds)
		{
			var all = _store.GetExpensesFiltered(userId, null, null, null);
			var results = new List<LastImportDatesForPaymentMethod>();
			foreach (var pmId in paymentMethodIds)
			{
				var forPm = all.Where(e => e.PaymentMethod == pmId && e.CreatedBy == ExpenseConstants.ImportCreatedBy).ToList();
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
