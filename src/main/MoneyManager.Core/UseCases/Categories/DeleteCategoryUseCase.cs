using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Categories
{
	public interface IDeleteCategoryUseCase
	{
		Task<CategoryDeleteResult> Execute(int id);
	}

	public class DeleteCategoryUseCase : IDeleteCategoryUseCase
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<DeleteCategoryUseCase> _logger;

		public DeleteCategoryUseCase(ICategoryRepository repository, ILogger<DeleteCategoryUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<CategoryDeleteResult> Execute(int id)
		{
			try
			{
				var result = await _repository.Delete(id);
				if (result.Success)
					_logger.LogInformation("Deleted category {CategoryId}", id);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete category {CategoryId}", id);
				return CategoryDeleteResult.Error("Failed to delete category.");
			}
		}
	}
}
