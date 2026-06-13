using Moq;
using MoneyManager.Core.Application.Categories.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Core.Tests.Handlers.Categories;

public class GetCategoryHandlerTests : HandlerBase<GetCategoryHandler>
{
	private Mock<ICategoryRepository> _repository => MockFor<ICategoryRepository>();

	protected readonly int _id;
	protected readonly CancellationToken _ct;
	protected Category? _result;
	protected Category _category = null!;
	protected Exception _expectedException = null!;
	protected Exception? _thrownException;

	public GetCategoryHandlerTests()
	{
		_id = Math.Abs(Fixture.Create<int>()) % 10000 + 1;
		_ct = CancellationToken.None;
		_category = Fixture.Create<Category>();
		_category.Category_I = _id;
	}

	public class Success_Setup : GetCategoryHandlerTests
	{
		public Success_Setup()
		{
			_repository.Setup(r => r.GetById(_id)).ReturnsAsync(_category);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetCategoryQuery(_id), _ct);
		}
	}

	public class Success : IClassFixture<Success_Setup>
	{
		private readonly Success_Setup _fixture;

		public Success(Success_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsCategory() => Assert.Same(_fixture._category, _fixture._result);
	}

	public class NotFound_Setup : GetCategoryHandlerTests
	{
		public NotFound_Setup()
		{
			_repository.Setup(r => r.GetById(_id)).ReturnsAsync((Category?)null);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_result = await SubjectUnderTest.Handle(new GetCategoryQuery(_id), _ct);
		}
	}

	public class NotFound : IClassFixture<NotFound_Setup>
	{
		private readonly NotFound_Setup _fixture;

		public NotFound(NotFound_Setup fixture) => _fixture = fixture;

		[Fact]
		public void ReturnsNull() => Assert.Null(_fixture._result);
	}

	public class RepositoryThrows_Setup : GetCategoryHandlerTests
	{
		public RepositoryThrows_Setup()
		{
			_expectedException = new InvalidOperationException("get failed");
			_repository.Setup(r => r.GetById(_id)).ThrowsAsync(_expectedException);
		}

		protected override async Task ExecuteTestMethodAsync()
		{
			_thrownException = await Record.ExceptionAsync(() =>
				SubjectUnderTest.Handle(new GetCategoryQuery(_id), _ct));
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
