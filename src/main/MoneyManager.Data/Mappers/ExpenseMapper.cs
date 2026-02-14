using MoneyManager.Data.Models;
using Microsoft.Data.SqlClient;

namespace MoneyManager.Data.Mappers
{
	public class ExpenseMapper : IExpenseMapper
	{
		public async Task<DbExpense> FromDbReader(SqlDataReader reader)
		{
			var paymentMethodOrdinal = reader.GetOrdinal("PaymentMethod");
			var categoryOrdinal = reader.GetOrdinal("Category");
			return new DbExpense
			{
				Expense_I = reader.GetInt32(reader.GetOrdinal("Expense_I")),
				ExpenseDate = reader.GetDateTime(reader.GetOrdinal("ExpenseDate")),
				Expense = reader.GetString(reader.GetOrdinal("Expense")),
				Amount = reader.GetDecimal(reader.GetOrdinal("Amount")),
				PaymentMethod = reader.IsDBNull(paymentMethodOrdinal) ? null : reader.GetInt32(paymentMethodOrdinal),
				Category = reader.IsDBNull(categoryOrdinal) ? null : reader.GetString(categoryOrdinal),
				DatePaid = reader.IsDBNull(reader.GetOrdinal("DatePaid")) ? null : reader.GetDateTime(reader.GetOrdinal("DatePaid")),
				UserId = reader.GetGuid(reader.GetOrdinal("UserId")),
				CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
				ModifiedDate = reader.IsDBNull(reader.GetOrdinal("ModifiedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("ModifiedDate"))
			};
		}
	}
}
