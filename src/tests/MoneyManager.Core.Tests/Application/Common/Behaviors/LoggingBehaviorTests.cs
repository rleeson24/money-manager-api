using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyManager.Core.Application.Common.Behaviors;
using Xunit;

namespace MoneyManager.Core.Tests.Application.Common.Behaviors;

public class LoggingBehaviorTests
{
	public record TestRequest : IRequest<string>;

	[Fact]
	public async Task ReturnsResponseOnSuccess()
	{
		var behavior = new LoggingBehavior<TestRequest, string>(NullLogger<LoggingBehavior<TestRequest, string>>.Instance);

		var result = await behavior.Handle(
			new TestRequest(),
			_ => Task.FromResult("done"),
			CancellationToken.None);

		Assert.Equal("done", result);
	}

	[Fact]
	public async Task RethrowsOnFailure()
	{
		var behavior = new LoggingBehavior<TestRequest, string>(NullLogger<LoggingBehavior<TestRequest, string>>.Instance);
		var expected = new InvalidOperationException("handler failed");

		var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			behavior.Handle(
				new TestRequest(),
				_ => throw expected,
				CancellationToken.None));

		Assert.Same(expected, thrown);
	}
}
