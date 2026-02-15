using MoneyManager.Core.Repositories;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Repositories;
using MoneyManager.Data.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyManager.Data
{
	public static class DependencyInjectionExtensions
	{
		public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Reader mappers (SqlDataReader -> Db*)
			services.AddScoped<IExpenseMapper, ExpenseMapper>();
			services.AddScoped<ICategoryMapper, CategoryMapper>();
			services.AddScoped<IPaymentMethodMapper, PaymentMethodMapper>();

			// Domain mapper (DbExpense <-> Core types, internal to Data)
			services.AddScoped<ExpenseDomainMapper>();

			// Repository implementations satisfy Core interfaces
			services.AddScoped<IExpenseRepository, ExpenseRepository>();
			services.AddScoped<ICategoryRepository, CategoryRepository>();
			services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();

			services.AddSingleton<DbExecutor>();
			services.AddSingleton(sp => new DbConnectionFactory(configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.")));

			return services;
		}
	}
}
