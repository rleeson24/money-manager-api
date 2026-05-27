using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.UseCases.Categories
{
	public interface ICreateCategoryUseCase
	{
		Task<CategoryMutationResult> Execute(CreateCategoryModel model);
	}

	public class CreateCategoryUseCase : ICreateCategoryUseCase
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<CreateCategoryUseCase> _logger;

		public CreateCategoryUseCase(ICategoryRepository repository, ILogger<CreateCategoryUseCase> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<CategoryMutationResult> Execute(CreateCategoryModel model)
		{
			try
			{
				var result = await _repository.Create(model);
				if (result.Category != null)
					_logger.LogInformation("Created category {CategoryId}: {Name}", result.Category.Category_I, result.Category.Name);
				return result;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create category");
				return CategoryMutationResult.Error("Failed to create category.");
			}
		}
	}
}
