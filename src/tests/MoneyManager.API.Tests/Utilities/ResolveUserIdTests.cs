using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using MoneyManager.API.Utilities;
using MoneyManager.Core.Constants;

namespace MoneyManager.API.Tests.Utilities;

public class ResolveUserIdTests
{
	private static ResolveUserId CreateSut(IConfiguration configuration) => new(configuration);

	private static IConfiguration CreateConfiguration(Dictionary<string, string?>? values = null)
	{
		return new ConfigurationBuilder()
			.AddInMemoryCollection(values ?? new Dictionary<string, string?>())
			.Build();
	}

	private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
		new(new ClaimsIdentity(claims, authenticationType: "test"));

	[Theory]
	[InlineData("oid")]
	[InlineData("http://schemas.microsoft.com/identity/claims/objectidentifier")]
	[InlineData(ClaimTypes.NameIdentifier)]
	[InlineData("sub")]
	public void Resolve_WithSupportedClaim_ReturnsUserId(string claimType)
	{
		var userId = Guid.NewGuid();
		var configuration = CreateConfiguration();
		var sut = CreateSut(configuration);
		var principal = CreatePrincipal(new Claim(claimType, userId.ToString()));

		var result = sut.Resolve(principal);

		Assert.Equal(userId, result);
	}

	[Fact]
	public void Resolve_WithInvalidGuidClaim_ReturnsNull()
	{
		var configuration = CreateConfiguration();
		var sut = CreateSut(configuration);
		var principal = CreatePrincipal(new Claim("oid", "not-a-guid"));

		var result = sut.Resolve(principal);

		Assert.Null(result);
	}

	[Fact]
	public void Resolve_WhenUnauthenticatedUnderAspire_ReturnsSeedUserId()
	{
		var configuration = CreateConfiguration(new Dictionary<string, string?>
		{
			["Data:AspireOrchestrated"] = "true"
		});
		var sut = CreateSut(configuration);
		var principal = new ClaimsPrincipal(new ClaimsIdentity());

		var result = sut.Resolve(principal);

		Assert.Equal(Guid.Parse(AspireConstants.DefaultSeedUserId), result);
	}

	[Fact]
	public void Resolve_WhenUnauthenticatedUnderAspire_UsesConfiguredSeedUserId()
	{
		var customSeed = Guid.NewGuid();
		var configuration = CreateConfiguration(new Dictionary<string, string?>
		{
			["Data:AspireOrchestrated"] = "true",
			["Data:AspireSeedUserId"] = customSeed.ToString()
		});
		var sut = CreateSut(configuration);
		var principal = new ClaimsPrincipal(new ClaimsIdentity());

		var result = sut.Resolve(principal);

		Assert.Equal(customSeed, result);
	}

	[Fact]
	public void Resolve_WhenNoClaimAndNotAspire_ReturnsNull()
	{
		var configuration = CreateConfiguration(new Dictionary<string, string?>
		{
			["Data:AspireOrchestrated"] = "false"
		});
		var sut = CreateSut(configuration);
		var principal = new ClaimsPrincipal(new ClaimsIdentity());

		var result = sut.Resolve(principal);

		Assert.Null(result);
	}
}
