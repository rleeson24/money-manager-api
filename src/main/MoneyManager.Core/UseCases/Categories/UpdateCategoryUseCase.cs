using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Categories
{
	public interface IUpdateCategoryUseCase
	{
		Task<CategoryMutationResult> Execute(int id, UpdateCategoryModel model);
	}

	public class UpdateCategoryUseCase : IUpdateCategoryUseCase
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<UpdateCategoryUseCase> _logger;

		public UpdateCategoryUseCase(ICategoryRepository repository, ILogger<UpdateCategoryUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<CategoryMutationResult> Execute(int id, UpdateCategoryModel model)
		{
			try
			{
				var result = await _repository.Update(id, model);
				if (result.Category != null)
					_logger.LogInformation("Updated category {CategoryId}", id);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to update category {CategoryId}", id);
				return CategoryMutationResult.Error("Failed to update category.");
			}
		}
	}
}
