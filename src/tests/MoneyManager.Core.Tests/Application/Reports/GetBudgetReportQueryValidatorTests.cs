using FluentValidation.TestHelper;
using MoneyManager.Core.Application.Reports.Queries;
using Xunit;

namespace MoneyManager.Core.Tests.Application.Reports;

public class GetBudgetReportQueryValidatorTests
{
	private readonly GetBudgetReportQueryValidator _validator = new();

	[Theory]
	[InlineData("2026-01")]
	[InlineData("2026-12")]
	public async Task AcceptsYearMonth(string month)
	{
		var result = await _validator.TestValidateAsync(new GetBudgetReportQuery(Guid.NewGuid(), month));

		result.ShouldNotHaveAnyValidationErrors();
	}

	[Theory]
	[InlineData("")]
	[InlineData("2026-13")]
	[InlineData("2026-00")]
	[InlineData("2026-1")]
	[InlineData("26-01")]
	[InlineData("2026/01")]
	public async Task RejectsInvalidMonth(string month)
	{
		var result = await _validator.TestValidateAsync(new GetBudgetReportQuery(Guid.NewGuid(), month));

		result.ShouldHaveValidationErrorFor(q => q.Month);
	}

	[Fact]
	public async Task RejectsEmptyUser()
	{
		var result = await _validator.TestValidateAsync(new GetBudgetReportQuery(Guid.Empty, "2026-01"));

		result.ShouldHaveValidationErrorFor(q => q.UserId);
	}
}
