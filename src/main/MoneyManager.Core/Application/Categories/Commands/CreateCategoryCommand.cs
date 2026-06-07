using FluentValidation;
using MediatR;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Categories.Commands
{
	public record CreateCategoryCommand(CreateCategoryModel Model) : IRequest<CategoryMutationResult>;

	public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CategoryMutationResult>
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<CreateCategoryHandler> _logger;

		public CreateCategoryHandler(ICategoryRepository repository, ILogger<CreateCategoryHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<CategoryMutationResult> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
		{
			try
			{
				var result = await _repository.Create(request.Model);
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

	public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
	{
		public CreateCategoryCommandValidator()
		{
			RuleFor(x => x.Model.Name).NotEmpty().MaximumLength(100);
		}
	}
}
