using FluentValidation;
using MediatR;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Categories.Commands
{
	public record DeleteCategoryCommand(int Id) : IRequest<CategoryDeleteResult>;

	public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, CategoryDeleteResult>
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<DeleteCategoryHandler> _logger;

		public DeleteCategoryHandler(ICategoryRepository repository, ILogger<DeleteCategoryHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<CategoryDeleteResult> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
		{
			var result = await _repository.Delete(request.Id);
			if (result.Success)
				_logger.LogInformation("Deleted category {CategoryId}", request.Id);
			return result;
		}
	}

	public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
	{
		public DeleteCategoryCommandValidator()
		{
			RuleFor(x => x.Id).GreaterThan(0);
		}
	}
}
