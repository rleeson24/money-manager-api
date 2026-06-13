using Microsoft.Data.SqlClient;
using MoneyManager.Core.Expenses;
using MoneyManager.Data.Expenses;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Data.Tests.Expenses;

public class ExpensePatchDbUpdateBuilderTests : TestBase<object>
{
	protected List<string> _setClauses = null!;
	protected List<SqlParameter> _parameters = null!;
	protected bool _hasChanges;

	public ExpensePatchDbUpdateBuilderTests()
	{
		_setClauses = new List<string>();
		_parameters = new List<SqlParameter>();
	}

	public class AppendPatchSetClauses : ExpensePatchDbUpdateBuilderTests
	{
		public AppendPatchSetClauses()
		{
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var updates = new Dictionary<string, object?>
			{
				[ExpenseFieldNames.ExpenseDate] = new DateTime(2026, 6, 1),
				[ExpenseFieldNames.Expense] = "Patched",
				[ExpenseFieldNames.Amount] = 42.50m,
				[ExpenseFieldNames.Currency] = "USD",
				[ExpenseFieldNames.PaymentMethod] = null,
				[ExpenseFieldNames.Category] = 10,
				[ExpenseFieldNames.DatePaid] = new DateTime(2026, 6, 2),
				[ExpenseFieldNames.IsSplit] = true,
				[ExpenseFieldNames.ExcludeFromCredit] = false
			};

			_hasChanges = ExpensePatchDbUpdateBuilder.AppendPatchSetClauses(updates, _setClauses, _parameters);
		}

		[Fact]
		public void ReturnsTrue_WhenFieldsPresent() => Assert.True(_hasChanges);

		[Fact]
		public void AddsExpenseDateClause() => Assert.Contains("ExpenseDate = @ExpenseDate", _setClauses);

		[Fact]
		public void AddsExpenseClause() => Assert.Contains("Expense = @Expense", _setClauses);

		[Fact]
		public void AddsAmountClause() => Assert.Contains("Amount = @Amount", _setClauses);

		[Fact]
		public void AddsCurrencyClause() => Assert.Contains("Currency = @Currency", _setClauses);

		[Fact]
		public void AddsPaymentMethodNullClause() => Assert.Contains("PaymentMethod = NULL", _setClauses);

		[Fact]
		public void AddsCategoryClause() => Assert.Contains("Category = @Category", _setClauses);

		[Fact]
		public void AddsDatePaidClause() => Assert.Contains("DatePaid = @DatePaid", _setClauses);

		[Fact]
		public void AddsIsSplitClause() => Assert.Contains("IsSplit = @IsSplit", _setClauses);

		[Fact]
		public void AddsExcludeFromCreditClause() => Assert.Contains("ExcludeFromCredit = @ExcludeFromCredit", _setClauses);

		[Fact]
		public void AddsMatchingParameters()
		{
			Assert.Contains(_parameters, p => p.ParameterName == "@ExpenseDate");
			Assert.Contains(_parameters, p => p.ParameterName == "@Expense" && (string)p.Value! == "Patched");
			Assert.Contains(_parameters, p => p.ParameterName == "@Amount" && (decimal)p.Value! == 42.50m);
			Assert.Contains(_parameters, p => p.ParameterName == "@IsSplit" && (bool)p.Value!);
		}
	}

	public class AppendPatchSetClauses_NullableNulls : ExpensePatchDbUpdateBuilderTests
	{
		public AppendPatchSetClauses_NullableNulls()
		{
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var updates = new Dictionary<string, object?>
			{
				[ExpenseFieldNames.Category] = null,
				[ExpenseFieldNames.DatePaid] = null
			};

			_hasChanges = ExpensePatchDbUpdateBuilder.AppendPatchSetClauses(updates, _setClauses, _parameters);
		}

		[Fact]
		public void AddsCategoryNullClause() => Assert.Contains("Category = NULL", _setClauses);

		[Fact]
		public void AddsDatePaidNullClause() => Assert.Contains("DatePaid = NULL", _setClauses);

		[Fact]
		public void DoesNotAddParametersForNullLiterals() => Assert.Empty(_parameters);
	}

	public class AppendPatchSetClauses_Empty : ExpensePatchDbUpdateBuilderTests
	{
		public AppendPatchSetClauses_Empty()
		{
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			_hasChanges = ExpensePatchDbUpdateBuilder.AppendPatchSetClauses(
				new Dictionary<string, object?>(),
				_setClauses,
				_parameters);
		}

		[Fact]
		public void ReturnsFalse_WhenNoUpdates() => Assert.False(_hasChanges);
	}

	public class AppendBulkSetClauses : ExpensePatchDbUpdateBuilderTests
	{
		public AppendBulkSetClauses()
		{
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var updates = new Dictionary<string, object?>
			{
				[ExpenseFieldNames.ExpenseDate] = new DateTime(2026, 7, 1),
				[ExpenseFieldNames.Category] = 15,
				[ExpenseFieldNames.DatePaid] = null
			};

			_hasChanges = ExpensePatchDbUpdateBuilder.AppendBulkSetClauses(updates, _setClauses, _parameters);
		}

		[Fact]
		public void ReturnsTrue_WhenFieldsPresent() => Assert.True(_hasChanges);

		[Fact]
		public void AddsBulkExpenseDateClause() => Assert.Contains("ExpenseDate = @ExpenseDate", _setClauses);

		[Fact]
		public void AddsBulkCategoryClause() => Assert.Contains("Category = @Category", _setClauses);

		[Fact]
		public void AddsBulkDatePaidNullClause() => Assert.Contains("DatePaid = NULL", _setClauses);

		[Fact]
		public void IgnoresNonBulkFields()
		{
			_setClauses.Clear();
			_parameters.Clear();

			var hasChanges = ExpensePatchDbUpdateBuilder.AppendBulkSetClauses(
				new Dictionary<string, object?> { [ExpenseFieldNames.Expense] = "Ignored" },
				_setClauses,
				_parameters);

			Assert.False(hasChanges);
			Assert.Empty(_setClauses);
		}
	}
}
