using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Reports;
using MoneyManager.Core.Reports;
using MoneyManager.Core.Repositories;

namespace MoneyManager.Core.Application.Reports.Queries
{
	public record GetBudgetReportQuery(Guid UserId, string Month) : IRequest<BudgetReport>;

	public class GetBudgetReportHandler : IRequestHandler<GetBudgetReportQuery, BudgetReport>
	{
		private readonly IExpenseRepository _expenses;
		private readonly IExpenseSplitRepository _splits;
		private readonly ICategoryRepository _categories;
		private readonly ILogger<GetBudgetReportHandler> _logger;

		public GetBudgetReportHandler(
			IExpenseRepository expenses,
			IExpenseSplitRepository splits,
			ICategoryRepository categories,
			ILogger<GetBudgetReportHandler> logger)
		{
			_expenses = expenses;
			_splits = splits;
			_categories = categories;
			_logger = logger;
		}

		public async Task<BudgetReport> Handle(GetBudgetReportQuery request, CancellationToken cancellationToken)
		{
			var expenses = await _expenses.ListForUser(request.UserId, request.Month);
			var splitExpenseIds = expenses
				.Where(e => e.IsSplit)
				.Select(e => e.Expense_I)
				.Distinct()
				.ToList();
			var splits = splitExpenseIds.Count == 0
				? Array.Empty<ExpenseSplit>()
				: await _splits.ListByExpenseIds(request.UserId, splitExpenseIds);
			var categories = await _categories.GetAll(activeOnly: false);
			var report = BudgetReportBuilder.Build(request.Month, expenses, splits, categories);
			_logger.LogDebug(
				"Built budget report for user {UserId} month {Month}: {CategoryCount} categories, spent {TotalSpent}",
				request.UserId,
				request.Month,
				report.Categories.Count,
				report.TotalSpent);
			return report;
		}
	}

	public class GetBudgetReportQueryValidator : AbstractValidator<GetBudgetReportQuery>
	{
		public GetBudgetReportQueryValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();
			RuleFor(x => x.Month)
				.NotEmpty()
				.Matches(@"^\d{4}-(0[1-9]|1[0-2])$")
				.WithMessage("Month must be yyyy-MM.");
		}
	}
}
