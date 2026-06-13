using Moq;
using MoneyManager.Core.Application.Categories.Commands;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Categories;

public class CreateCategoryHandlerTests : HandlerBase<CreateCategoryHandler>
{
	private Mock<ICategoryRepository> _repository => MockFor<ICategoryRepository>();

	protected readonly CancellationToken _ct;
	protected CreateCategoryModel _model = null!;
	protected CategoryMutationResult _result = null!;
	protected Category _category = null!;
	protected string _errorMessage = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public CreateCategoryHandlerTests()
	{
		_ct = CancellationToken.None;
		_model = Fixture.Create<CreateCategoryModel>();
		_category = Fixture.Create<Category>();
	}

	public class Success_Setup : CreateCategoryHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.Create(_model)).ReturnsAsync(CategoryMutationResult.Success(_category));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new CreateCategoryCommand(_model), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsCategory() => Assert.Same(_fixture._category, _fixture._result.Category);
	}

	public class ValidationError_Setup : CreateCategoryHandlerTests
	{
		public ValidationError_Setup()
		{
			_errorMessage = "Parent category 99 not found.";
			_repository.Setup(r => r.Create(_model)).ReturnsAsync(CategoryMutationResult.Error(_errorMessage));
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new CreateCategoryCommand(_model), _ct);
		}
	}

	public class ValidationError : IClassFixture<ValidationError_Setup>
	{
		private readonly ValidationError_Setup _fixture;

		public ValidationError(ValidationError_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsValidationError() => Assert.Equal(_fixture._errorMessage, _fixture._result.ValidationError);
	}

	public class RepositoryThrows_Setup : CreateCategoryHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("create failed");
			_repository.Setup(r => r.Create(_model)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new CreateCategoryCommand(_model), _ct));
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
