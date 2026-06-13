using AutoFixture;

namespace MoneyManager.Tests.Utilities;

/// <summary>
/// AutoFixture's default <see cref="DateOnly"/> specimen uses random (year, month, day) and often throws
/// <see cref="ArgumentOutOfRangeException"/>. Derive calendar dates from generated <see cref="DateTime"/> instead.
/// </summary>
public static class ConfiguredFixture
{
	public static IFixture Create()
	{
		var f = new Fixture();
		f.Customize<DateOnly>(c => c.FromFactory(() => DateOnly.FromDateTime(f.Create<DateTime>())));
		return f;
	}
}
