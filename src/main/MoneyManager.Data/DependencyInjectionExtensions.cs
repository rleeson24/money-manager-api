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
			var runningUnderAspire = AspireOrchestrationDetector.IsRunningUnderAspire(configuration);
			var connectionString = configuration.GetConnectionString("DefaultConnection");

			// Under Aspire always use SQL + bootstrap even when appsettings.Development has UseInMemoryDatabase=true.
			if (dataOptions.UseInMemoryDatabase && string.IsNullOrEmpty(connectionString) && !runningUnderAspire)
			{
				services.AddSingleton<InMemoryStore>();
				services.AddScoped<IExpenseRepository, InMemoryExpenseRepository>();
				services.AddScoped<ICategoryRepository, InMemoryCategoryRepository>();
				services.AddScoped<IPaymentMethodRepository, InMemoryPaymentMethodRepository>();
				services.AddScoped<IExpenseSplitRepository, InMemoryExpenseSplitRepository>();
				return services;
			}

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

			if (runningUnderAspire)
			{
				services.AddSingleton<DbConnectionFactory>(sp =>
				{
					var config = sp.GetRequiredService<IConfiguration>();
					var aspireConnectionString = config.GetConnectionString("DefaultConnection");
					if (string.IsNullOrEmpty(aspireConnectionString))
					{
						throw new InvalidOperationException(
							"Connection string 'DefaultConnection' not found while running under Aspire. "
							+ "Ensure AppHost references the sql database with WithReference(sql).");
					}

					return new DbConnectionFactory(
						SqlConnectionStringHelper.ApplyAspireSqlContainerDefaults(aspireConnectionString));
				});
				services.AddHostedService<AspireSqlDevelopmentBootstrap>();
				return services;
			}

			if (string.IsNullOrEmpty(connectionString))
			{
				throw new InvalidOperationException(
					"Connection string 'DefaultConnection' not found. For development without SQL Server, set Data:UseInMemoryDatabase to true.");
			}

			services.AddSingleton(new DbConnectionFactory(connectionString));
			return services;
		}
	}
}
