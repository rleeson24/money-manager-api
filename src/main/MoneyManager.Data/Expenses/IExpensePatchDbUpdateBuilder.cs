using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Expenses
{
	public interface IExpensePatchDbUpdateBuilder
	{
		bool AppendPatchSetClauses(
			IReadOnlyDictionary<string, object?> updates,
			ICollection<string> setClauses,
			ICollection<SqlParameter> parameters);

		bool AppendBulkSetClauses(
			IReadOnlyDictionary<string, object?> updates,
			ICollection<string> setClauses,
			ICollection<SqlParameter> parameters);
	}
}
