using FluentValidation;
using MediatR;
using MoneyManager.Core.Application.Categories;
using MoneyManager.Core.Application.Categories.Commands;
using MoneyManager.Core.Application.Common.Behaviors;
using MoneyManager.Core.Expenses;
using MoneyManager.Core.Import;
using MoneyManager.Core.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyManager.Core
{
	public static class DependencyInjectionExtensions
	{
		public static IServiceCollection AddCoreServices(this IServiceCollection services)
		{
			services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateCategoryHandler).Assembly));
			services.AddValidatorsFromAssembly(typeof(CreateCategoryCommandValidator).Assembly);
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
			services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

			services.AddSingleton<ICategoryValidator, CategoryValidator>();
			services.AddSingleton<IImportDuplicateFilter, ImportDuplicateFilter>();
			services.AddSingleton<IImportTransactionFilter, ImportTransactionFilter>();
			services.AddSingleton<IImportTransactionNormalizer, ImportTransactionNormalizer>();
			services.AddSingleton<ICategoryTreeService, CategoryTreeService>();
			services.AddSingleton<IExpensePatchParser, ExpensePatchParser>();
			services.AddSingleton<IExpensePatchApplicator, ExpensePatchApplicator>();
			services.AddSingleton<IExpenseBulkUpdateMapper, ExpenseBulkUpdateMapper>();

			return services;
		}
	}
}
