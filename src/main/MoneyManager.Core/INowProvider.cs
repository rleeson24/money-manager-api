namespace MoneyManager.Core
{
	public interface INowProvider
	{
		DateTime UtcNow { get; }
	}
}
