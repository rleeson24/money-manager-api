using FluentValidation;
using MediatR;
using MoneyManager.Core.Application.Categories;
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
		public DeleteCategoryCommandValidator(ICategoryRepository repository)
		{
			RuleFor(x => x.Id).GreaterThan(0);
			RuleFor(x => x).CustomAsync(async (command, context, cancellationToken) =>
			{
				var existing = await repository.GetAll();
				var current = existing.FirstOrDefault(c => c.Category_I == command.Id);
				if (current == null)
					return;

				var inUse = await repository.IsInUse(command.Id);
				var error = CategoryCommandValidationRules.ValidateDelete(current, existing, inUse);
				if (error != null)
					context.AddFailure(error);
			});
		}
	}
}
