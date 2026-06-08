using MediatR;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Categories.Queries
{
	public record GetCategoryQuery(int Id) : IRequest<Category?>;

	public class GetCategoryHandler : IRequestHandler<GetCategoryQuery, Category?>
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<GetCategoryHandler> _logger;

		public GetCategoryHandler(ICategoryRepository repository, ILogger<GetCategoryHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<Category?> Handle(GetCategoryQuery request, CancellationToken cancellationToken)
		{
			return await _repository.GetById(request.Id);
		}
	}
}
