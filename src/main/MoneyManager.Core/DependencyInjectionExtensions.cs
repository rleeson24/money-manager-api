using MoneyManager.Core.Repositories;
using MoneyManager.Core.UseCases.Categories;
using MoneyManager.Core.UseCases.Expenses;
using MoneyManager.Core.UseCases.PaymentMethods;
using MoneyManager.Core.UseCases.ExpenseSplits;
using MoneyManager.Core.UseCases.Import;
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
			services.AddScoped<IGetCategoryUseCase, GetCategoryUseCase>();
			services.AddScoped<ICreateCategoryUseCase, CreateCategoryUseCase>();
			services.AddScoped<IUpdateCategoryUseCase, UpdateCategoryUseCase>();
			services.AddScoped<IDeleteCategoryUseCase, DeleteCategoryUseCase>();
			services.AddScoped<IGetPaymentMethodsUseCase, GetPaymentMethodsUseCase>();
			services.AddScoped<IGetExpenseSplitsUseCase, GetExpenseSplitsUseCase>();
			services.AddScoped<ICreateExpenseSplitUseCase, CreateExpenseSplitUseCase>();
			services.AddScoped<IUpdateExpenseSplitUseCase, UpdateExpenseSplitUseCase>();
			services.AddScoped<IDeleteExpenseSplitUseCase, DeleteExpenseSplitUseCase>();
			services.AddScoped<IReplaceExpenseSplitsUseCase, ReplaceExpenseSplitsUseCase>();

			services.AddScoped<IImportFromFileUseCase, ImportFromFileUseCase>();
			services.AddScoped<IGetLastImportDatesUseCase, GetLastImportDatesUseCase>();

			return services;
		}
	}
}
