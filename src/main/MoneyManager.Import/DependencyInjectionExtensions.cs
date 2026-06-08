using MoneyManager.Core.Import;
using MoneyManager.Import.Parsers;
using Microsoft.Extensions.DependencyInjection;

namespace MoneyManager.Import
{
	public static class DependencyInjectionExtensions
	{
		public static IServiceCollection AddImportParsers(this IServiceCollection services)
		{
			services.AddScoped<ICsvTransactionParser, DiscoverCreditCsvParser>();
			services.AddScoped<ICsvTransactionParser, ArvestCsvParser>();
			services.AddScoped<ICsvTransactionParser, AbfcuCsvParser>();
			services.AddScoped<ICsvTransactionParser, DiscoverBankCsvParser>();
			services.AddScoped<ICsvParserSelector, CsvParserSelector>();
			services.AddScoped<ITransactionFileParser, CompositeTransactionFileParser>();
			return services;
		}
	}
}
