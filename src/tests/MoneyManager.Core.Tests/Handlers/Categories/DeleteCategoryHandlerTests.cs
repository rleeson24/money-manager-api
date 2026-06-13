using Moq;
using MoneyManager.Core.Application.Categories.Commands;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Categories;

public class DeleteCategoryHandlerTests : HandlerBase<DeleteCategoryHandler>
{
	private Mock<ICategoryRepository> _repository => MockFor<ICategoryRepository>();

	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected CategoryDeleteResult _result = null!;
	protected string _errorMessage = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public DeleteCategoryHandlerTests()
	{
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
	}

	public class Success_Setup : DeleteCategoryHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Delete(_id)).ReturnsAsync(CategoryDeleteResult.Ok());
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new DeleteCategoryCommand(_id), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsSuccess() => Assert.True(_fixture._result.Success);
	}

	public class NotFound_Setup : DeleteCategoryHandlerTests
	{
		public NotFound_Setup()
		{
			_repository.Setup(r => r.Delete(_id)).ReturnsAsync(CategoryDeleteResult.NotFound());
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new DeleteCategoryCommand(_id), _ct);
		}
	}

	public class NotFound : IClassFixture<NotFound_Setup>
	{
		private readonly NotFound_Setup _fixture;

		public NotFound(NotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsNotFound() => Assert.True(_fixture._result.IsNotFound);
	}

	public class ValidationError_Setup : DeleteCategoryHandlerTests
	{
		public ValidationError_Setup()
		{
			_errorMessage = "Cannot delete a category that is used by expenses. Archive it instead.";
			_repository.Setup(r => r.Delete(_id)).ReturnsAsync(CategoryDeleteResult.Error(_errorMessage));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new DeleteCategoryCommand(_id), _ct);
		}
	}

	public class ValidationError : IClassFixture<ValidationError_Setup>
	{
		private readonly ValidationError_Setup _fixture;

		public ValidationError(ValidationError_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsValidationError() => Assert.Equal(_fixture._errorMessage, _fixture._result.ValidationError);
	}

	public class RepositoryThrows_Setup : DeleteCategoryHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("delete failed");
			_repository.Setup(r => r.Delete(_id)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new DeleteCategoryCommand(_id), _ct));
		}
	}

	public class RepositoryThrows : IClassFixture<RepositoryThrows_Setup>
	{
		private readonly RepositoryThrows_Setup _fixture;

		public RepositoryThrows(RepositoryThrows_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ThrowsExpectedException() => Assert.Same(_fixture._expectedException, _fixture._thrownException);
	}
}
