using System.Text;

namespace MoneyManager.Import.Tests
{
	internal static class CsvFixtureHelper
	{
		public static Stream OpenFixture(string fileName)
		{
			var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
			return File.OpenRead(path);
		}

		public static Stream ToStream(string csvContent)
		{
			var bytes = Encoding.UTF8.GetBytes(csvContent);
			return new MemoryStream(bytes);
		}
	}
}
