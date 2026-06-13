using Moq;
using MoneyManager.Core.Application.Categories.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Categories;

public class GetCategoriesHandlerTests : HandlerBase<GetCategoriesHandler>
{
	private Mock<ICategoryRepository> _repository => MockFor<ICategoryRepository>();

	protected readonly CancellationToken _ct;
	protected IReadOnlyList<Category>? _result;
	protected IReadOnlyList<Category> _categories = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public GetCategoriesHandlerTests()
	{
		_ct = CancellationToken.None;
		_categories = Fixture.CreateMany<Category>(4).ToList();
	}

	public class Success_Setup : GetCategoriesHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.GetAll(false)).ReturnsAsync(_categories);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetCategoriesQuery(), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsCategories() => Assert.Same(_fixture._categories, _fixture._result);

		[Fact]
		public void CallsGetAllInactiveIncluded()
		{
			_fixture._repository.Verify(r => r.GetAll(false), Times.Once);
		}
	}

	public class ActiveOnly_Setup : GetCategoriesHandlerTests
	{
		public ActiveOnly_Setup()
		{
			_repository.Setup(r => r.GetAll(true)).ReturnsAsync(_categories);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetCategoriesQuery(ActiveOnly: true), _ct);
		}
	}

	public class ActiveOnly : IClassFixture<ActiveOnly_Setup>
	{
		private readonly ActiveOnly_Setup _fixture;

		public ActiveOnly(ActiveOnly_Setup fixture) => _fixture = fixture;

		[Fact]
		public void CallsGetAllActiveOnly()
		{
			_fixture._repository.Verify(r => r.GetAll(true), Times.Once);
		}
	}

	public class RepositoryThrows_Setup : GetCategoriesHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("list failed");
			_repository.Setup(r => r.GetAll(false)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new GetCategoriesQuery(), _ct));
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
