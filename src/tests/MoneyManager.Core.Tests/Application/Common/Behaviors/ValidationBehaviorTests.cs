using FluentValidation;
using MediatR;
using MoneyManager.Core.Application.Common.Behaviors;
using Xunit;

namespace MoneyManager.Core.Tests.Application.Common.Behaviors;

public class ValidationBehaviorTests
{
	public record TestRequest(string Value) : IRequest<string>;

	private class TestValidator : AbstractValidator<TestRequest>
	{
		public TestValidator() => RuleFor(x => x.Value).NotEmpty();
	}

	private class FailingValidatorA : AbstractValidator<TestRequest>
	{
		public FailingValidatorA() => RuleFor(x => x.Value).Must(_ => false).WithMessage("error A");
	}

	private class FailingValidatorB : AbstractValidator<TestRequest>
	{
		public FailingValidatorB() => RuleFor(x => x.Value).Must(_ => false).WithMessage("error B");
	}

	[Fact]
	public async Task PassesThroughWhenNoValidators()
	{
		var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());
		var called = false;

		var result = await behavior.Handle(
			new TestRequest(""),
			_ =>
			{
				called = true;
				return Task.FromResult("ok");
			},
			CancellationToken.None);

		Assert.True(called);
		Assert.Equal("ok", result);
	}

	[Fact]
	public async Task CallsNextWhenValidationPasses()
	{
		var validator = new TestValidator();
		var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });
		var called = false;

		var result = await behavior.Handle(
			new TestRequest("valid"),
			_ =>
			{
				called = true;
				return Task.FromResult("ok");
			},
			CancellationToken.None);

		Assert.True(called);
		Assert.Equal("ok", result);
	}

	[Fact]
	public async Task ThrowsValidationExceptionWhenInvalid()
	{
		var validator = new TestValidator();
		var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });

		await Assert.ThrowsAsync<ValidationException>(() =>
			behavior.Handle(
				new TestRequest(""),
				_ => Task.FromResult("ok"),
				CancellationToken.None));
	}

	[Fact]
	public async Task AggregatesFailuresFromMultipleValidators()
	{
		var behavior = new ValidationBehavior<TestRequest, string>(
			new IValidator<TestRequest>[] { new FailingValidatorA(), new FailingValidatorB() });

		var ex = await Assert.ThrowsAsync<ValidationException>(() =>
			behavior.Handle(new TestRequest("x"), _ => Task.FromResult("ok"), CancellationToken.None));

		Assert.Contains(ex.Errors, e => e.ErrorMessage == "error A");
		Assert.Contains(ex.Errors, e => e.ErrorMessage == "error B");
	}
}
