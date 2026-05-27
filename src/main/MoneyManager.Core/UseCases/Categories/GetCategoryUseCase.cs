using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Categories
{
	public interface IGetCategoryUseCase
	{
		Task<Category?> Execute(int id);
	}

	public class GetCategoryUseCase : IGetCategoryUseCase
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<GetCategoryUseCase> _logger;

		public GetCategoryUseCase(ICategoryRepository repository, ILogger<GetCategoryUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<Category?> Execute(int id)
		{
			try
			{
				return await _repository.GetById(id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to fetch category {CategoryId}", id);
				return null;
			}
		}
	}
}
