using MoneyManager.Core.Mappers;
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
			// Mappers
			services.AddScoped<IExpenseMapper, ExpenseMapper>();
			services.AddScoped<ICategoryMapper, CategoryMapper>();
			services.AddScoped<IPaymentMethodMapper, PaymentMethodMapper>();

			// Expense Use Cases
			services.AddScoped<IGetExpensesUseCase, GetExpensesUseCase>();
			services.AddScoped<IGetExpenseUseCase, GetExpenseUseCase>();
			services.AddScoped<ICreateExpenseUseCase, CreateExpenseUseCase>();
			services.AddScoped<IUpdateExpenseUseCase, UpdateExpenseUseCase>();
			services.AddScoped<IDeleteExpenseUseCase, DeleteExpenseUseCase>();
			services.AddScoped<IPatchExpenseUseCase, PatchExpenseUseCase>();
			services.AddScoped<IBulkUpdateExpensesUseCase, BulkUpdateExpensesUseCase>();
			services.AddScoped<IBulkDeleteExpensesUseCase, BulkDeleteExpensesUseCase>();

			// Category Use Cases
			services.AddScoped<IGetCategoriesUseCase, GetCategoriesUseCase>();

			// Payment Method Use Cases
			services.AddScoped<IGetPaymentMethodsUseCase, GetPaymentMethodsUseCase>();

			return services;
		}
	}
}
