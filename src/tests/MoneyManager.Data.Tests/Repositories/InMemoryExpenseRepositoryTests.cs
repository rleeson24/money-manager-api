using MoneyManager.Core.Expenses;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Data.Repositories;
using Xunit;

namespace MoneyManager.Data.Tests.Repositories;

public class InMemoryExpenseRepositoryTests
{
	private readonly InMemoryRepositoryFixture _fixture = new();

	public class Get
	{
		private readonly InMemoryExpenseRepository _repository;
		private readonly Guid _userId;

		public Get()
		{
			_fixture = new InMemoryRepositoryFixture();
			_repository = _fixture.CreateExpenseRepository();
			_userId = _fixture.SeedUserId;
		}

		private readonly InMemoryRepositoryFixture _fixture;

		[Fact]
		public async Task ReturnsExpense_WhenOwnedByUser()
		{
			var result = await _repository.Get(1, _userId);

			Assert.NotNull(result);
			Assert.Equal(1, result!.Expense_I);
			Assert.Equal("COPA AIRLINES PANAMA PAN", result.ExpenseDescription);
		}

		[Fact]
		public async Task ReturnsNull_WhenNotOwnedByUser()
		{
			var result = await _repository.Get(1, _fixture.OtherUserId);

			Assert.Null(result);
		}

		[Fact]
		public async Task ReturnsNull_WhenIdDoesNotExist()
		{
			var result = await _repository.Get(9999, _userId);

			Assert.Null(result);
		}
	}

	public class Create
	{
		private readonly InMemoryExpenseRepository _repository;
		private readonly Guid _userId;
		private readonly CreateExpenseModel _model;
		private readonly InMemoryRepositoryFixture _fixture;

		public Create()
		{
			_fixture = new InMemoryRepositoryFixture();
			_repository = _fixture.CreateExpenseRepository();
			_userId = _fixture.SeedUserId;
			_model = new CreateExpenseModel
			{
				ExpenseDate = new DateTime(2026, 6, 1),
				Expense = "New expense",
				Amount = 50m,
				PaymentMethod = 1,
				Category = 6
			};
		}

		[Fact]
		public async Task AssignsIdAndPersistsExpense()
		{
			var result = await _repository.Create(_userId, _model);

			Assert.NotNull(result);
			Assert.True(result!.Expense_I > 0);
			Assert.Equal(_model.Expense, result.ExpenseDescription);
			Assert.Equal(_fixture.FixedUtcNow, result.CreatedDateTime);
			Assert.Equal(_fixture.FixedUtcNow, result.ModifiedDateTime);

			var stored = await _repository.Get(result.Expense_I, _userId);
			Assert.NotNull(stored);
			Assert.Equal(result.Expense_I, stored!.Expense_I);
		}
	}

	public class Update
	{
		private readonly InMemoryExpenseRepository _repository;
		private readonly Guid _userId;

		public Update()
		{
			var fixture = new InMemoryRepositoryFixture();
			_repository = fixture.CreateExpenseRepository();
			_userId = fixture.SeedUserId;
		}

		[Fact]
		public async Task ReturnsSuccess_WhenModifiedDateMatches()
		{
			var existing = await _repository.Get(1, _userId);
			Assert.NotNull(existing);

			var updated = new Expense
			{
				Expense_I = existing!.Expense_I,
				ExpenseDate = new DateTime(2026, 6, 5),
				ExpenseDescription = "Updated description",
				Amount = 200m,
				Currency = "USD",
				PaymentMethod = existing.PaymentMethod,
				Category = existing.Category,
				DatePaid = existing.DatePaid,
				CreatedDateTime = existing.CreatedDateTime,
				ModifiedDateTime = existing.ModifiedDateTime,
				IsSplit = existing.IsSplit,
				ExcludeFromCredit = existing.ExcludeFromCredit,
				CreatedBy = existing.CreatedBy
			};

			var result = await _repository.Update(1, _userId, updated);

			Assert.True(result.IsSuccess);
			Assert.Equal("Updated description", result.Updated!.ExpenseDescription);
			Assert.Equal(200m, result.Updated.Amount);
		}

		[Fact]
		public async Task ReturnsConflict_WhenModifiedDateDiffers()
		{
			var existing = await _repository.Get(1, _userId);
			Assert.NotNull(existing);

			var stale = new Expense
			{
				Expense_I = existing!.Expense_I,
				ExpenseDate = existing.ExpenseDate,
				ExpenseDescription = "Stale",
				Amount = existing.Amount,
				ModifiedDateTime = existing.ModifiedDateTime.AddHours(-1),
				CreatedDateTime = existing.CreatedDateTime,
				CreatedBy = existing.CreatedBy
			};

			var result = await _repository.Update(1, _userId, stale);

			Assert.True(result.IsConflict);
			Assert.Equal(existing.Expense_I, result.ConflictCurrent!.Expense_I);
		}

		[Fact]
		public async Task ReturnsNotFound_WhenExpenseMissing()
		{
			var result = await _repository.Update(9999, _userId, new Expense
			{
				ExpenseDate = DateTime.UtcNow,
				ExpenseDescription = "Missing",
				Amount = 1m,
				ModifiedDateTime = DateTime.UtcNow
			});

			Assert.True(result.IsNotFound);
		}

		[Fact]
		public async Task ReturnsNotFound_WhenUserDoesNotOwnExpense()
		{
			var existing = await _repository.Get(1, _userId);
			Assert.NotNull(existing);

			var result = await _repository.Update(1, Guid.NewGuid(), existing!);

			Assert.True(result.IsNotFound);
		}
	}

	public class Patch
	{
		private readonly InMemoryExpenseRepository _repository;
		private readonly Guid _userId;
		private readonly InMemoryRepositoryFixture _fixture;

		public Patch()
		{
			_fixture = new InMemoryRepositoryFixture();
			_repository = _fixture.CreateExpenseRepository();
			_userId = _fixture.SeedUserId;
		}

		[Fact]
		public async Task AppliesUpdates_WhenModifiedDateMatches()
		{
			var existing = await _repository.Get(2, _userId);
			Assert.NotNull(existing);

			var updates = new Dictionary<string, object?>
			{
				[ExpenseFieldNames.Expense] = "Patched split expense",
				[ExpenseFieldNames.Amount] = 10m
			};

			var result = await _repository.Patch(2, _userId, updates, existing!.ModifiedDateTime);

			Assert.True(result.IsSuccess);
			Assert.Equal("Patched split expense", result.Updated!.ExpenseDescription);
			Assert.Equal(10m, result.Updated.Amount);
			Assert.Equal(_fixture.FixedUtcNow, result.Updated.ModifiedDateTime);
		}

		[Fact]
		public async Task ReturnsConflict_WhenModifiedDateDiffers()
		{
			var existing = await _repository.Get(2, _userId);
			Assert.NotNull(existing);

			var result = await _repository.Patch(
				2,
				_userId,
				new Dictionary<string, object?> { [ExpenseFieldNames.Expense] = "Conflict" },
				existing!.ModifiedDateTime.AddMinutes(-5));

			Assert.True(result.IsConflict);
		}

		[Fact]
		public async Task ReturnsNotFound_WhenExpenseMissing()
		{
			var result = await _repository.Patch(
				9999,
				_userId,
				new Dictionary<string, object?> { [ExpenseFieldNames.Expense] = "Missing" },
				null);

			Assert.True(result.IsNotFound);
		}
	}

	public class Delete
	{
		private readonly InMemoryExpenseRepository _repository;
		private readonly Guid _userId;

		public Delete()
		{
			var fixture = new InMemoryRepositoryFixture();
			_store = fixture.CreateStore();
			_repository = fixture.CreateExpenseRepository(_store);
			_userId = fixture.SeedUserId;
		}

		private readonly InMemoryStore _store;

		[Fact]
		public async Task RemovesExpense_WhenOwnedByUser()
		{
			var created = await _repository.Create(_userId, new CreateExpenseModel
			{
				ExpenseDate = new DateTime(2026, 6, 2),
				Expense = "To delete",
				Amount = 5m
			});
			Assert.NotNull(created);

			var deleted = await _repository.Delete(created!.Expense_I, _userId);

			Assert.True(deleted);
			Assert.Null(await _repository.Get(created.Expense_I, _userId));
		}

		[Fact]
		public async Task ReturnsFalse_WhenUserDoesNotOwnExpense()
		{
			var deleted = await _repository.Delete(1, Guid.NewGuid());

			Assert.False(deleted);
		}
	}

	public class BulkUpdate
	{
		private readonly InMemoryExpenseRepository _repository;
		private readonly Guid _userId;
		private readonly InMemoryRepositoryFixture _fixture;

		public BulkUpdate()
		{
			_fixture = new InMemoryRepositoryFixture();
			_repository = _fixture.CreateExpenseRepository();
			_userId = _fixture.SeedUserId;
		}

		[Fact]
		public async Task UpdatesMultipleExpenses()
		{
			var newDate = new DateTime(2026, 8, 1);
			var updates = new Dictionary<string, object?>
			{
				[ExpenseFieldNames.ExpenseDate] = newDate,
				[ExpenseFieldNames.Category] = 99
			};

			var result = await _repository.BulkUpdate(new[] { 1, 2 }, _userId, updates);

			Assert.True(result);

			var first = await _repository.Get(1, _userId);
			var second = await _repository.Get(2, _userId);
			Assert.Equal(newDate, first!.ExpenseDate);
			Assert.Equal(99, first.Category);
			Assert.Equal(newDate, second!.ExpenseDate);
			Assert.Equal(_fixture.FixedUtcNow, first.ModifiedDateTime);
		}

		[Fact]
		public async Task ReturnsFalse_WhenIdsEmpty()
		{
			var result = await _repository.BulkUpdate(
				Array.Empty<int>(),
				_userId,
				new Dictionary<string, object?> { [ExpenseFieldNames.Category] = 1 });

			Assert.False(result);
		}

		[Fact]
		public async Task ReturnsFalse_WhenUpdatesEmpty()
		{
			var result = await _repository.BulkUpdate(new[] { 1 }, _userId, new Dictionary<string, object?>());

			Assert.False(result);
		}
	}

	public class BulkDelete
	{
		private readonly InMemoryExpenseRepository _repository;
		private readonly Guid _userId;

		public BulkDelete()
		{
			var fixture = new InMemoryRepositoryFixture();
			_repository = fixture.CreateExpenseRepository();
			_userId = fixture.SeedUserId;
		}

		[Fact]
		public async Task RemovesMultipleExpenses()
		{
			var result = await _repository.BulkDelete(new[] { 3, 4 }, _userId);

			Assert.True(result);
			Assert.Null(await _repository.Get(3, _userId));
			Assert.Null(await _repository.Get(4, _userId));
		}

		[Fact]
		public async Task ReturnsFalse_WhenIdsEmpty()
		{
			var result = await _repository.BulkDelete(Array.Empty<int>(), _userId);

			Assert.False(result);
		}
	}

	public class UserScoping
	{
		private readonly InMemoryExpenseRepository _repository;
		private readonly InMemoryRepositoryFixture _fixture;

		public UserScoping()
		{
			_fixture = new InMemoryRepositoryFixture();
			_repository = _fixture.CreateExpenseRepository();
		}

		[Fact]
		public async Task ListForUser_ReturnsOnlySeedUserExpenses()
		{
			var seedExpenses = await _repository.ListForUser(_fixture.SeedUserId);
			var otherExpenses = await _repository.ListForUser(_fixture.OtherUserId);

			Assert.Equal(9, seedExpenses.Count);
			Assert.Empty(otherExpenses);
		}

		[Fact]
		public async Task Create_AssignsExpenseToRequestingUser()
		{
			var created = await _repository.Create(_fixture.OtherUserId, new CreateExpenseModel
			{
				ExpenseDate = new DateTime(2026, 6, 3),
				Expense = "Other user expense",
				Amount = 15m
			});
			Assert.NotNull(created);

			Assert.Null(await _repository.Get(created!.Expense_I, _fixture.SeedUserId));
			Assert.NotNull(await _repository.Get(created.Expense_I, _fixture.OtherUserId));
		}
	}
}
