using System.Collections.Generic;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Categories
{
	public interface IGetCategoriesUseCase
	{
		Task<IReadOnlyList<Category>?> Execute();
	}

	public class GetCategoriesUseCase : IGetCategoriesUseCase
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<GetCategoriesUseCase> _logger;

		public GetCategoriesUseCase(ICategoryRepository repository, ILogger<GetCategoriesUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<IReadOnlyList<Category>?> Execute()
		{
			try
			{
				var categories = await _repository.GetAll();
				_logger.LogDebug("Fetched {Count} categories", categories.Count);
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
