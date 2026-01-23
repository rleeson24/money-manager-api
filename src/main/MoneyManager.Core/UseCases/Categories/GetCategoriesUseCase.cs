using MoneyManager.Core.Models;
using MoneyManager.Core.Mappers;
using MoneyManager.Data.Repositories;
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
		private readonly ICategoryMapper _mapper;

		public GetCategoriesUseCase(ICategoryRepository repository, ILogger<GetCategoriesUseCase> logger, ICategoryMapper mapper)
		{
			_repository = repository;
			_logger = logger;
			_mapper = mapper;
		}

		public async Task<IEnumerable<Category>?> Execute()
		{
			try
			{
				var categories = await _repository.GetAll();
				return categories.Select(c => _mapper.DbToOutput(c));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occurred fetching categories");
				return null;
			}
		}
	}
}
