using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Categories
{
	public interface IGetCategoriesUseCase
	{
		Task<IEnumerable<Category>?> Execute();
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

		public async Task<IEnumerable<Category>?> Execute()
		{
			try
			{
				return await _repository.GetAll();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching categories");
				return null;
			}
		}
	}
}
