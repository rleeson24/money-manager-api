using MoneyManager.Core.Models;

namespace MoneyManager.Core.Expenses
{
	public interface IExpensePatchApplicator
	{
		Expense Apply(Expense current, IReadOnlyDictionary<string, object?> updates, DateTime modifiedUtc);

		void ApplyTo(Expense target, IReadOnlyDictionary<string, object?> updates, DateTime modifiedUtc);
	}
}
