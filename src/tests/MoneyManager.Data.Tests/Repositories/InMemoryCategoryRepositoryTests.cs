using MoneyManager.Core.Models.Input;
using MoneyManager.Data.Repositories;
using Xunit;

namespace MoneyManager.Data.Tests.Repositories;

public class InMemoryCategoryRepositoryTests
{
	public class GetAll
	{
		private readonly InMemoryCategoryRepository _repository;

		public GetAll()
		{
			_repository = new InMemoryRepositoryFixture().CreateCategoryRepository();
		}

		[Fact]
		public async Task ReturnsSeededCategories()
		{
			var categories = await _repository.GetAll();

			Assert.NotEmpty(categories);
		}

		[Fact]
		public async Task ActiveOnly_ExcludesArchivedCategories()
		{
			var fixture = new InMemoryRepositoryFixture();
			var store = fixture.CreateStore();
			var repository = fixture.CreateCategoryRepository(store);
			var created = await repository.Create(new CreateCategoryModel { Name = "Archived test category" });
			Assert.NotNull(created.Category);

			await repository.Update(created.Category!.Category_I, new UpdateCategoryModel { Archived = true });

			var active = await repository.GetAll(activeOnly: true);
			var all = await repository.GetAll(activeOnly: false);

			Assert.DoesNotContain(active, c => c.Category_I == created.Category.Category_I);
			Assert.Contains(all, c => c.Category_I == created.Category.Category_I && c.Archived);
		}
	}

	public class GetById
	{
		private readonly InMemoryCategoryRepository _repository;

		public GetById()
		{
			_repository = new InMemoryRepositoryFixture().CreateCategoryRepository();
		}

		[Fact]
		public async Task ReturnsCategory_WhenExists()
		{
			var all = await _repository.GetAll();
			var first = all[0];

			var result = await _repository.GetById(first.Category_I);

			Assert.NotNull(result);
			Assert.Equal(first.Name, result!.Name);
		}

		[Fact]
		public async Task ReturnsNull_WhenMissing()
		{
			var result = await _repository.GetById(999999);

			Assert.Null(result);
		}
	}

	public class Create
	{
		private readonly InMemoryCategoryRepository _repository;

		public Create()
		{
			_repository = new InMemoryRepositoryFixture().CreateCategoryRepository();
		}

		[Fact]
		public async Task CreatesCategory_WithTrimmedName()
		{
			var result = await _repository.Create(new CreateCategoryModel
			{
				Name = "  Test Category  ",
				ParentCategory_I = null,
				Required = true
			});

			Assert.Null(result.ValidationError);
			Assert.NotNull(result.Category);
			Assert.Equal("Test Category", result.Category!.Name);
			Assert.True(result.Category.Required);
			Assert.False(result.Category.Archived);
		}
	}

	public class Update
	{
		private readonly InMemoryCategoryRepository _repository;

		public Update()
		{
			_repository = new InMemoryRepositoryFixture().CreateCategoryRepository();
		}

		[Fact]
		public async Task UpdatesCategoryFields()
		{
			var created = await _repository.Create(new CreateCategoryModel { Name = "Original" });
			Assert.NotNull(created.Category);

			var result = await _repository.Update(created.Category!.Category_I, new UpdateCategoryModel
			{
				Name = "Renamed",
				Required = true,
				Archived = true
			});

			Assert.NotNull(result.Category);
			Assert.Equal("Renamed", result.Category!.Name);
			Assert.True(result.Category.Required);
			Assert.True(result.Category.Archived);
		}

		[Fact]
		public async Task ClearsParent_WhenClearParentSet()
		{
			var created = await _repository.Create(new CreateCategoryModel
			{
				Name = "Child",
				ParentCategory_I = 1
			});
			Assert.NotNull(created.Category);

			var result = await _repository.Update(created.Category!.Category_I, new UpdateCategoryModel
			{
				ClearParent = true
			});

			Assert.NotNull(result.Category);
			Assert.Null(result.Category!.ParentCategory_I);
		}

		[Fact]
		public async Task ReturnsNotFound_WhenCategoryMissing()
		{
			var result = await _repository.Update(999999, new UpdateCategoryModel { Name = "Missing" });

			Assert.True(result.IsNotFound);
		}
	}

	public class Delete
	{
		private readonly InMemoryCategoryRepository _repository;

		public Delete()
		{
			_repository = new InMemoryRepositoryFixture().CreateCategoryRepository();
		}

		[Fact]
		public async Task DeletesCategory_WhenExists()
		{
			var created = await _repository.Create(new CreateCategoryModel { Name = "Disposable" });
			Assert.NotNull(created.Category);

			var result = await _repository.Delete(created.Category!.Category_I);

			Assert.True(result.Success);
			Assert.Null(await _repository.GetById(created.Category.Category_I));
		}

		[Fact]
		public async Task ReturnsNotFound_WhenCategoryMissing()
		{
			var result = await _repository.Delete(999999);

			Assert.True(result.IsNotFound);
		}
	}

	public class IsInUse
	{
		private readonly InMemoryCategoryRepository _repository;
		private readonly InMemoryExpenseRepository _expenseRepository;
		private readonly Guid _userId;

		public IsInUse()
		{
			var fixture = new InMemoryRepositoryFixture();
			var store = fixture.CreateStore();
			_repository = fixture.CreateCategoryRepository(store);
			_expenseRepository = fixture.CreateExpenseRepository(store);
			_userId = fixture.SeedUserId;
		}

		[Fact]
		public async Task ReturnsTrue_WhenCategoryReferencedByExpense()
		{
			var inUse = await _repository.IsInUse(42);

			Assert.True(inUse);
		}

		[Fact]
		public async Task ReturnsFalse_WhenCategoryUnused()
		{
			var created = await _repository.Create(new CreateCategoryModel { Name = "Unused category" });
			Assert.NotNull(created.Category);

			var inUse = await _repository.IsInUse(created.Category!.Category_I);

			Assert.False(inUse);
		}
	}
}
