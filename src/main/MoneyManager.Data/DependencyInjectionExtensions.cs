using MoneyManager.Core;
using MoneyManager.Core.Repositories;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Repositories;
using MoneyManager.Data.Bootstrap;
using MoneyManager.Data.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MoneyManager.Data
{
	public static class DependencyInjectionExtensions
	{
		public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<DataOptions>(configuration.GetSection("Data"));

			services.AddSingleton<INowProvider, SystemNowProvider>();

			var dataOptions = configuration.GetSection("Data").Get<DataOptions>() ?? new DataOptions();
			var connectionString = configuration.GetConnectionString("DefaultConnection");
			// Aspire (and other hosts) inject a connection string; prefer SQL when present even if UseInMemoryDatabase is true in appsettings.
			if (dataOptions.UseInMemoryDatabase && string.IsNullOrEmpty(connectionString))
			{
				// In-memory database for development: no SQL Server required.
				services.AddSingleton<InMemoryStore>();
				services.AddScoped<IExpenseRepository, InMemoryExpenseRepository>();
				services.AddScoped<ICategoryRepository, InMemoryCategoryRepository>();
				services.AddScoped<IPaymentMethodRepository, InMemoryPaymentMethodRepository>();
				services.AddScoped<IExpenseSplitRepository, InMemoryExpenseSplitRepository>();
				return services;
			}

			// SQL Server: reader mappers and repositories
			services.AddScoped<IExpenseMapper, ExpenseMapper>();
			services.AddScoped<ICategoryMapper, CategoryMapper>();
			services.AddScoped<IPaymentMethodMapper, PaymentMethodMapper>();
			services.AddScoped<IExpenseSplitMapper, ExpenseSplitMapper>();
			services.AddScoped<ExpenseDomainMapper>();
			services.AddScoped<IExpenseRepository, ExpenseRepository>();
			services.AddScoped<ICategoryRepository, CategoryRepository>();
			services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
			services.AddScoped<IExpenseSplitRepository, ExpenseSplitRepository>();

			services.AddSingleton<DbExecutor>();
			if (string.IsNullOrEmpty(connectionString))
				throw new InvalidOperationException("Connection string 'DefaultConnection' not found. For development without SQL Server, set Data:UseInMemoryDatabase to true.");
			if (AspireOrchestrationDetector.IsRunningUnderAspire(configuration))
				connectionString = SqlConnectionStringHelper.ApplyAspireSqlContainerDefaults(connectionString);
			services.AddSingleton(new DbConnectionFactory(connectionString));

			if (AspireOrchestrationDetector.IsRunningUnderAspire(configuration))
				services.AddHostedService<AspireSqlDevelopmentBootstrap>();

			return services;
		}
	}
}
