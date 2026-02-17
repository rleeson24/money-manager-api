using MoneyManager.Core;

namespace MoneyManager.Data.Utilities
{
	public class SystemNowProvider : INowProvider
	{
		public DateTime UtcNow => DateTime.UtcNow;
	}
}
