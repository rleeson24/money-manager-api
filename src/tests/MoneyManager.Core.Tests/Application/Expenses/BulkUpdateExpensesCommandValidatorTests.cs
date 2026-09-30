using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models.Input;
using Xunit;

namespace MoneyManager.Core.Tests.Application.Expenses;

public class BulkUpdateExpensesCommandValidatorTests
{
	private readonly BulkUpdateExpensesCommandValidator _validator = new(new ExpenseBulkUpdateMapper());

	[Fact]
	public void AcceptsExcludeFromCredit()
	{
		var command = new BulkUpdateExpensesCommand(
			Guid.NewGuid(),
			new BulkUpdateRequest { Ids = [1], ExcludeFromCredit = false });

		var result = _validator.Validate(command);

		Assert.True(result.IsValid);
	}
}
