using Moq;
using MoneyManager.Core.Application.Categories.Commands;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Categories;

public class UpdateCategoryHandlerTests : HandlerBase<UpdateCategoryHandler>
{
	private Mock<ICategoryRepository> _repository => MockFor<ICategoryRepository>();

	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected UpdateCategoryModel _model = null!;
	protected CategoryMutationResult _result = null!;
	protected Category _category = null!;
	protected string _errorMessage = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public UpdateCategoryHandlerTests()
	{
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
		_model = Fixture.Create<UpdateCategoryModel>();
		_category = Fixture.Create<Category>();
		_category.Category_I = _id;
	}

	public class Success_Setup : UpdateCategoryHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Update(_id, _model)).ReturnsAsync(CategoryMutationResult.Success(_category));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new UpdateCategoryCommand(_id, _model), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsUpdatedCategory() => Assert.Same(_fixture._category, _fixture._result.Category);
	}

	public class NotFound_Setup : UpdateCategoryHandlerTests
	{
		public NotFound_Setup()
		{
			_repository.Setup(r => r.Update(_id, _model)).ReturnsAsync(CategoryMutationResult.NotFound());
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new UpdateCategoryCommand(_id, _model), _ct);
		}
	}

	public class NotFound : IClassFixture<NotFound_Setup>
	{
		private readonly NotFound_Setup _fixture;

		public NotFound(NotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void IsNotFound() => Assert.True(_fixture._result.IsNotFound);
	}

	public class ValidationError_Setup : UpdateCategoryHandlerTests
	{
		public ValidationError_Setup()
		{
			_errorMessage = "Cannot assign a parent to a category that has children.";
			_repository.Setup(r => r.Update(_id, _model)).ReturnsAsync(CategoryMutationResult.Error(_errorMessage));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new UpdateCategoryCommand(_id, _model), _ct);
		}
	}

	public class ValidationError : IClassFixture<ValidationError_Setup>
	{
		private readonly ValidationError_Setup _fixture;

		public ValidationError(ValidationError_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsValidationError() => Assert.Equal(_fixture._errorMessage, _fixture._result.ValidationError);
	}

	public class RepositoryThrows_Setup : UpdateCategoryHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("update failed");
			_repository.Setup(r => r.Update(_id, _model)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new UpdateCategoryCommand(_id, _model), _ct));
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
