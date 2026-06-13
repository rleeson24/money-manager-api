using Xunit;

namespace MoneyManager.Tests.Utilities;

/// <summary>
/// Base class for MediatR handler unit tests. Inherits subject-under-test construction from <see cref="TestBase{TSubject}"/>.
/// Implements <see cref="IAsyncLifetime"/>; <see cref="InitializeAsync"/> runs <see cref="TestBase{TSubject}.ExecuteTestMethodAsync"/>.
/// Child classes configure mocks and test data in their constructor; that runs before xUnit calls <see cref="InitializeAsync"/>.
/// </summary>
/// <typeparam name="TSubject">The handler under test (e.g. UpdateExpenseHandler).</typeparam>
public abstract class HandlerBase<TSubject> : TestBase<TSubject>, IAsyncLifetime
	where TSubject : class
{
	public async Task InitializeAsync()
	{
		BuildSubject();
		await ExecuteTestMethodAsync();
	}

	public virtual Task DisposeAsync() => Task.CompletedTask;
}
