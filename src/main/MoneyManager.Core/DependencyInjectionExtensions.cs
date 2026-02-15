using MoneyManager.Core.Repositories;
using MoneyManager.Core.UseCases.Categories;
using MoneyManager.Core.UseCases.Expenses;
using MoneyManager.Core.UseCases.PaymentMethods;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyManager.Core
{
	public static class DependencyInjectionExtensions
	{
		public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Use cases only; repository implementations are registered by the Data layer
			services.AddScoped<IGetExpensesUseCase, GetExpensesUseCase>();
			services.AddScoped<IGetExpenseUseCase, GetExpenseUseCase>();
			services.AddScoped<ICreateExpenseUseCase, CreateExpenseUseCase>();
			services.AddScoped<IUpdateExpenseUseCase, UpdateExpenseUseCase>();
			services.AddScoped<IDeleteExpenseUseCase, DeleteExpenseUseCase>();
			services.AddScoped<IPatchExpenseUseCase, PatchExpenseUseCase>();
			services.AddScoped<IBulkUpdateExpensesUseCase, BulkUpdateExpensesUseCase>();
			services.AddScoped<IBulkDeleteExpensesUseCase, BulkDeleteExpensesUseCase>();
			services.AddScoped<IGetCategoriesUseCase, GetCategoriesUseCase>();
			services.AddScoped<IGetPaymentMethodsUseCase, GetPaymentMethodsUseCase>();

			return services;
		}
	}
}
