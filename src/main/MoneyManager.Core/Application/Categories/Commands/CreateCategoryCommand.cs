using FluentValidation;
using MediatR;
using MoneyManager.Core.Application.Categories;
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
			var result = await _repository.Create(request.Model);
			if (result.Category != null)
				_logger.LogInformation("Created category {CategoryId}: {Name}", result.Category.Category_I, result.Category.Name);
			return result;
		}
	}

	public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
	{
		public CreateCategoryCommandValidator(ICategoryRepository repository, ICategoryValidator categoryValidator)
		{
			RuleFor(x => x.Model.Name).NotEmpty().MaximumLength(100);
			RuleFor(x => x.Model).CustomAsync(async (model, context, cancellationToken) =>
			{
				var existing = await repository.GetAll();
				var error = categoryValidator.ValidateCreate(model, existing);
				if (error != null)
					context.AddFailure(error);
			});
		}
	}
}
