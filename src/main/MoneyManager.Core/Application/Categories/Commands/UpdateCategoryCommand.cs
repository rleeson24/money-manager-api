using FluentValidation;
using MediatR;
using MoneyManager.Core.Application.Categories;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace MoneyManager.Core.Application.Categories.Commands
{
	public record UpdateCategoryCommand(int Id, UpdateCategoryModel Model) : IRequest<CategoryMutationResult>;

	public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryMutationResult>
	{
		private readonly ICategoryRepository _repository;
		private readonly ILogger<UpdateCategoryHandler> _logger;

		public UpdateCategoryHandler(ICategoryRepository repository, ILogger<UpdateCategoryHandler> logger)
		{
			_repository = repository;
			_logger = logger;
		}

		public async Task<CategoryMutationResult> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
		{
			var result = await _repository.Update(request.Id, request.Model);
			if (result.Category != null)
				_logger.LogInformation("Updated category {CategoryId}", request.Id);
			return result;
		}
	}

	public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
	{
		public UpdateCategoryCommandValidator(ICategoryRepository repository, ICategoryValidator categoryValidator)
		{
			RuleFor(x => x.Id).GreaterThan(0);
			When(x => x.Model.Name != null, () =>
			{
				RuleFor(x => x.Model.Name!).NotEmpty().MaximumLength(100);
			});
			RuleFor(x => x).CustomAsync(async (command, context, cancellationToken) =>
			{
				var existing = await repository.GetAll();
				var current = existing.FirstOrDefault(c => c.Category_I == command.Id);
				if (current == null)
					return;

				var error = categoryValidator.ValidateUpdate(current, command.Model, existing);
				if (error != null)
					context.AddFailure(error);
			});
		}
	}
}
