using FluentValidation;
using MediatR;
using MoneyManager.Core.Application.Categories.Commands;
using MoneyManager.Core.Application.Common.Behaviors;
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

			return services;
		}
	}
}
