using FluentValidation;
using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Expenses.Queries
{
	public record GetExpensesQuery(
		Guid UserId,
		string? Month = null,
		int? PaymentMethod = null,
		bool? DatePaidNull = null,
		string? Currency = null,
		DateTime? FromDate = null,
		DateTime? ToDate = null,
		string? Search = null,
		int? Category = null,
		bool IncludeChildCategories = false) : IRequest<IReadOnlyList<Expense>?>;

	public class GetExpensesHandler : IRequestHandler<GetExpensesQuery, IReadOnlyList<Expense>?>
	{
		private readonly IExpenseRepository _repository;
		private readonly ICategoryRepository _categoryRepository;
		private readonly ILogger<GetExpensesHandler> _logger;

		public GetExpensesHandler(
			IExpenseRepository repository,
			ICategoryRepository categoryRepository,
			ILogger<GetExpensesHandler> logger)
		{
			_repository = repository;
			_categoryRepository = categoryRepository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<Expense>?> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
		{
			if (request.FromDate.HasValue || request.ToDate.HasValue)
			{
				var categoryIds = await ResolveCategoryIds(request.Category, request.IncludeChildCategories);
				var expenses = await _repository.SearchForUser(
					request.UserId,
					request.FromDate!.Value,
					request.ToDate!.Value,
					string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
					categoryIds);
				_logger.LogDebug(
					"Searched {Count} expenses for user {UserId} (from={FromDate}, to={ToDate})",
					expenses.Count, request.UserId, request.FromDate, request.ToDate);
				return expenses;
			}

			var useFilters = request.PaymentMethod.HasValue
				|| request.DatePaidNull.HasValue
				|| !string.IsNullOrWhiteSpace(request.Currency);

			if (useFilters)
			{
				var expenses = await _repository.ListForUserWithFilters(
					request.UserId, request.PaymentMethod, request.DatePaidNull, request.Currency);
				_logger.LogDebug(
					"Fetched {Count} filtered expenses for user {UserId}",
					expenses.Count, request.UserId);
				return expenses;
			}

			var list = await _repository.ListForUser(request.UserId, request.Month);
			_logger.LogDebug(
				"Fetched {Count} expenses for user {UserId} (month={Month})",
				list.Count, request.UserId, request.Month ?? "all");
			return list;
		}

		private async Task<IReadOnlyList<int>?> ResolveCategoryIds(int? categoryId, bool includeChildCategories)
		{
			if (!categoryId.HasValue)
				return null;

			var ids = new List<int> { categoryId.Value };
			if (!includeChildCategories)
				return ids;

			var category = await _categoryRepository.GetById(categoryId.Value);
			if (category?.HasChildren != true)
				return ids;

			var all = await _categoryRepository.GetAll(activeOnly: false);
			ids.AddRange(all
				.Where(c => c.ParentCategory_I == categoryId.Value)
				.Select(c => c.Category_I));
			return ids;
		}
	}

	public class GetExpensesQueryValidator : AbstractValidator<GetExpensesQuery>
	{
		public GetExpensesQueryValidator()
		{
			RuleFor(x => x.UserId).NotEmpty();

			RuleFor(x => x)
				.Must(query =>
				{
					var useLegacyFilters = query.PaymentMethod.HasValue
						|| query.DatePaidNull.HasValue
						|| !string.IsNullOrWhiteSpace(query.Currency);
					return !(useLegacyFilters && !string.IsNullOrWhiteSpace(query.Month));
				})
				.WithMessage("Cannot combine month with payment method, datePaidNull, or currency filters.");

			RuleFor(x => x)
				.Must(query => !(query.FromDate.HasValue ^ query.ToDate.HasValue))
				.WithMessage("Both fromDate and toDate are required for date range search.");

			RuleFor(x => x)
				.Must(query =>
				{
					if (!query.FromDate.HasValue)
						return true;
					return query.FromDate!.Value.Date <= query.ToDate!.Value.Date;
				})
				.WithMessage("fromDate must be on or before toDate.");

			RuleFor(x => x)
				.Must(query =>
				{
					if (!query.FromDate.HasValue)
						return true;
					var hasSearch = !string.IsNullOrWhiteSpace(query.Search);
					var hasCategory = query.Category.HasValue;
					return hasSearch || hasCategory;
				})
				.WithMessage("Provide a search term or category when searching by date range.");

			RuleFor(x => x)
				.Must(query =>
				{
					if (!query.FromDate.HasValue)
						return true;
					var useLegacyFilters = query.PaymentMethod.HasValue
						|| query.DatePaidNull.HasValue
						|| !string.IsNullOrWhiteSpace(query.Currency);
					return !useLegacyFilters && string.IsNullOrWhiteSpace(query.Month);
				})
				.WithMessage("Date range search cannot be combined with month or other list filters.");
		}
	}
}
