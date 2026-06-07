using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Categories.Queries
{
	public record GetCategoriesQuery(bool ActiveOnly = false) : IRequest<IReadOnlyList<Category>?>;

	public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>?>
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<GetCategoriesHandler> _logger;

		public GetCategoriesHandler(ICategoryRepository repository, ILogger<GetCategoriesHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<Category>?> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var categories = await _repository.GetAll(request.ActiveOnly);
				_logger.LogDebug("Fetched {Count} categories (activeOnly={ActiveOnly})", categories.Count, request.ActiveOnly);
				return categories;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch categories");
				return null;
			}
		}
	}
}
