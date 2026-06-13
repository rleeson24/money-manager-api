using MoneyManager.Core.Models.Input;
using MoneyManager.Data.Repositories;
using Xunit;

namespace MoneyManager.Data.Tests.Repositories;

public class InMemoryExpenseSplitRepositoryTests
{
	public class GetByExpenseId
	{
		private readonly InMemoryExpenseSplitRepository _repository;
		private readonly Guid _userId;

		public GetByExpenseId()
		{
			var fixture = new InMemoryRepositoryFixture();
			_repository = fixture.CreateExpenseSplitRepository();
			_userId = fixture.SeedUserId;
		}

		[Fact]
		public async Task ReturnsEmpty_WhenNoSplitsExist()
		{
			var splits = await _repository.GetByExpenseId(1, _userId);

			Assert.Empty(splits);
		}

		[Fact]
		public async Task ReturnsEmpty_WhenUserDoesNotOwnExpense()
		{
			var splits = await _repository.GetByExpenseId(1, Guid.NewGuid());

			Assert.Empty(splits);
		}
	}

	public class Create
	{
		private readonly InMemoryExpenseSplitRepository _repository;
		private readonly Guid _userId;

		public Create()
		{
			var fixture = new InMemoryRepositoryFixture();
			_repository = fixture.CreateExpenseSplitRepository();
			_userId = fixture.SeedUserId;
		}

		[Fact]
		public async Task CreatesSplit_WhenExpenseOwnedByUser()
		{
			var model = new CreateOrUpdateExpenseSplitModel
			{
				Expense_I = 2,
				Description = "Custard share",
				Amount = 2.75m,
				Category = 19
			};

			var result = await _repository.Create(_userId, model);

			Assert.NotNull(result);
			Assert.True(result!.Id > 0);
			Assert.Equal(model.Description, result.Description);
			Assert.Equal(model.Amount, result.Amount);
		}

		[Fact]
		public async Task ReturnsNull_WhenExpenseNotOwnedByUser()
		{
			var result = await _repository.Create(Guid.NewGuid(), new CreateOrUpdateExpenseSplitModel
			{
				Expense_I = 2,
				Description = "Unauthorized",
				Amount = 1m,
				Category = 1
			});

			Assert.Null(result);
		}
	}

	public class Update
	{
		private readonly InMemoryExpenseSplitRepository _repository;
		private readonly Guid _userId;

		public Update()
		{
			var fixture = new InMemoryRepositoryFixture();
			_repository = fixture.CreateExpenseSplitRepository();
			_userId = fixture.SeedUserId;
		}

		[Fact]
		public async Task UpdatesSplit_WhenExists()
		{
			var created = await _repository.Create(_userId, new CreateOrUpdateExpenseSplitModel
			{
				Expense_I = 2,
				Description = "Original",
				Amount = 3m,
				Category = 19
			});
			Assert.NotNull(created);

			var updated = await _repository.Update(created!.Id, _userId, new CreateOrUpdateExpenseSplitModel
			{
				Expense_I = 2,
				Description = "Updated split",
				Amount = 4.5m,
				Category = 20
			});

			Assert.NotNull(updated);
			Assert.Equal("Updated split", updated!.Description);
			Assert.Equal(4.5m, updated.Amount);
			Assert.Equal(created.CreatedDateTime, updated.CreatedDateTime);
		}

		[Fact]
		public async Task ReturnsNull_WhenSplitMissing()
		{
			var result = await _repository.Update(99999, _userId, new CreateOrUpdateExpenseSplitModel
			{
				Expense_I = 2,
				Description = "Missing",
				Amount = 1m,
				Category = 1
			});

			Assert.Null(result);
		}
	}

	public class Delete
	{
		private readonly InMemoryExpenseSplitRepository _repository;
		private readonly Guid _userId;

		public Delete()
		{
			var fixture = new InMemoryRepositoryFixture();
			_repository = fixture.CreateExpenseSplitRepository();
			_userId = fixture.SeedUserId;
		}

		[Fact]
		public async Task RemovesSplit_WhenExists()
		{
			var created = await _repository.Create(_userId, new CreateOrUpdateExpenseSplitModel
			{
				Expense_I = 2,
				Description = "To delete",
				Amount = 1.5m,
				Category = 19
			});
			Assert.NotNull(created);

			var deleted = await _repository.Delete(created!.Id, _userId);

			Assert.True(deleted);
			Assert.Null(await _repository.Get(created.Id, _userId));
		}

		[Fact]
		public async Task ReturnsFalse_WhenSplitMissing()
		{
			var deleted = await _repository.Delete(99999, _userId);

			Assert.False(deleted);
		}
	}

	public class ReplaceByExpenseId
	{
		private readonly InMemoryExpenseSplitRepository _repository;
		private readonly Guid _userId;

		public ReplaceByExpenseId()
		{
			var fixture = new InMemoryRepositoryFixture();
			_repository = fixture.CreateExpenseSplitRepository();
			_userId = fixture.SeedUserId;
		}

		[Fact]
		public async Task ReplacesExistingSplits_WhenAmountsMatchParent()
		{
			await _repository.Create(_userId, new CreateOrUpdateExpenseSplitModel
			{
				Expense_I = 2,
				Description = "Old split",
				Amount = 5.51m,
				Category = 19
			});

			var result = await _repository.ReplaceByExpenseId(
				2,
				_userId,
				5.51m,
				new[]
				{
					new ReplaceExpenseSplitItemModel { Description = "Food", Amount = 3.01m, Category = 19 },
					new ReplaceExpenseSplitItemModel { Description = "Tip", Amount = 2.50m, Category = 19 }
				});

			Assert.True(result.IsSuccess);
			Assert.Equal(2, result.Splits!.Count);

			var stored = await _repository.GetByExpenseId(2, _userId);
			Assert.Equal(2, stored.Count);
			Assert.DoesNotContain(stored, s => s.Description == "Old split");
		}

		[Fact]
		public async Task ReturnsFailure_WhenAmountsDoNotMatchParent()
		{
			var result = await _repository.ReplaceByExpenseId(
				2,
				_userId,
				5.51m,
				new[]
				{
					new ReplaceExpenseSplitItemModel { Description = "Food", Amount = 3m, Category = 19 },
					new ReplaceExpenseSplitItemModel { Description = "Tip", Amount = 2m, Category = 19 }
				});

			Assert.False(result.IsSuccess);
			Assert.Equal("Split amounts must add up to the expense total.", result.ValidationError);
		}

		[Fact]
		public async Task ReturnsFailure_WhenExpenseNotOwnedByUser()
		{
			var result = await _repository.ReplaceByExpenseId(
				2,
				Guid.NewGuid(),
				5.51m,
				Array.Empty<ReplaceExpenseSplitItemModel>());

			Assert.False(result.IsSuccess);
			Assert.Equal("Expense not found.", result.ValidationError);
		}

		[Fact]
		public async Task ClearsSplits_WhenItemsEmpty()
		{
			await _repository.Create(_userId, new CreateOrUpdateExpenseSplitModel
			{
				Expense_I = 2,
				Description = "To clear",
				Amount = 1m,
				Category = 19
			});

			var result = await _repository.ReplaceByExpenseId(2, _userId, 5.51m, Array.Empty<ReplaceExpenseSplitItemModel>());

			Assert.True(result.IsSuccess);
			Assert.Empty(result.Splits!);
			Assert.Empty(await _repository.GetByExpenseId(2, _userId));
		}
	}
}
