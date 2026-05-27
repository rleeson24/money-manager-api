using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;
using MoneyManager.Core.Utilities;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using DataOptions = MoneyManager.Data.DataOptions;

namespace MoneyManager.Data.Repositories
{
	public class CategoryRepository : ICategoryRepository
	{
		private readonly DbExecutor _db;
		private readonly ICategoryMapper _readerMapper;
		private readonly DataOptions _dataOptions;

		public CategoryRepository(DbExecutor db, ICategoryMapper readerMapper, IOptions<DataOptions> dataOptions)
		{
			_db = db;
			_readerMapper = readerMapper;
			_dataOptions = dataOptions.Value;
		}

		public Task<IReadOnlyList<Category>> GetAll(bool activeOnly = false)
		{
			if (_dataOptions.UseMockData)
			{
				var list = LegacyCategorySeed.Categories.AsEnumerable();
				if (activeOnly)
					list = list.Where(c => !c.Archived);
				return Task.FromResult(FinalizeList(list));
			}

			return GetAllFromDb(activeOnly);
		}

		public async Task<Category?> GetById(int id)
		{
			if (_dataOptions.UseMockData)
			{
				var all = FinalizeList(LegacyCategorySeed.Categories);
				return all.FirstOrDefault(c => c.Category_I == id);
			}

			var result = new List<DbCategory>();
			await _db.ExecuteReader(
				"SELECT * FROM Categories WHERE Category_I = @Id",
				[new SqlParameter("@Id", id)],
				async sqlReader =>
				{
					while (await sqlReader.ReadAsync())
						result.Add(await _readerMapper.FromDbReader(sqlReader));
				});
			if (result.Count == 0)
				return null;

			var allFromDb = await GetAllFromDb(false);
			return allFromDb.FirstOrDefault(c => c.Category_I == id);
		}

		public async Task<CategoryMutationResult> Create(CreateCategoryModel model)
		{
			var existing = await GetAll();
			var validation = CategoryValidator.ValidateCreate(model, existing);
			if (validation != null)
				return CategoryMutationResult.Error(validation);

			var sql = @"INSERT INTO Categories (Name, ParentCategory_I, Required, Archived)
				VALUES (@Name, @ParentCategory_I, @Required, 0);
				SELECT CAST(SCOPE_IDENTITY() AS INT);";
			var scalar = await _db.ExecuteScalar(sql,
			[
				new SqlParameter("@Name", model.Name.Trim()),
				new SqlParameter("@ParentCategory_I", (object?)model.ParentCategory_I ?? DBNull.Value),
				new SqlParameter("@Required", model.Required)
			]);
			if (scalar == null)
				return CategoryMutationResult.NotFound();

			var id = Convert.ToInt32(scalar);
			return CategoryMutationResult.Success((await GetById(id))!);
		}

		public async Task<CategoryMutationResult> Update(int id, UpdateCategoryModel model)
		{
			var existing = await GetAll();
			var current = existing.FirstOrDefault(c => c.Category_I == id);
			if (current == null)
				return CategoryMutationResult.NotFound();

			var validation = CategoryValidator.ValidateUpdate(current, model, existing);
			if (validation != null)
				return CategoryMutationResult.Error(validation);

			var name = model.Name?.Trim() ?? current.Name;
			int? parent = current.ParentCategory_I;
			if (model.ClearParent == true)
				parent = null;
			else if (model.ParentCategory_I.HasValue)
				parent = model.ParentCategory_I;
			var required = model.Required ?? current.Required;
			var archived = model.Archived ?? current.Archived;

			await _db.ExecuteNonQuery(
				@"UPDATE Categories SET Name = @Name, ParentCategory_I = @ParentCategory_I,
					Required = @Required, Archived = @Archived WHERE Category_I = @Id",
				[
					new SqlParameter("@Id", id),
					new SqlParameter("@Name", name),
					new SqlParameter("@ParentCategory_I", (object?)parent ?? DBNull.Value),
					new SqlParameter("@Required", required),
					new SqlParameter("@Archived", archived)
				]);

			return CategoryMutationResult.Success((await GetById(id))!);
		}

		public async Task<CategoryDeleteResult> Delete(int id)
		{
			var existing = await GetAll();
			var current = existing.FirstOrDefault(c => c.Category_I == id);
			if (current == null)
				return CategoryDeleteResult.NotFound();

			var inUse = await IsInUse(id);
			var validation = CategoryValidator.ValidateDelete(current, existing, inUse);
			if (validation != null)
				return CategoryDeleteResult.Error(validation);

			await _db.ExecuteNonQuery("DELETE FROM Categories WHERE Category_I = @Id", [new SqlParameter("@Id", id)]);
			return CategoryDeleteResult.Ok();
		}

		public async Task<bool> IsInUse(int categoryId)
		{
			var scalar = await _db.ExecuteScalar(
				@"SELECT CASE WHEN EXISTS (SELECT 1 FROM Expenses WHERE Category = @Id)
					OR EXISTS (SELECT 1 FROM Expenses_split WHERE Category = @Id)
					THEN 1 ELSE 0 END",
				[new SqlParameter("@Id", categoryId)]);
			return scalar != null && Convert.ToInt32(scalar) == 1;
		}

		private async Task<IReadOnlyList<Category>> GetAllFromDb(bool activeOnly)
		{
			var where = activeOnly ? " WHERE c.Archived = 0" : "";
			var sql = $@"SELECT c.* FROM Categories c
				LEFT JOIN Categories p ON c.ParentCategory_I = p.Category_I
				{where}
				ORDER BY COALESCE(p.Name, c.Name), c.Name";

			var result = new List<DbCategory>();
			await _db.ExecuteReader(sql, [],
				async sqlReader =>
				{
					while (await sqlReader.ReadAsync())
						result.Add(await _readerMapper.FromDbReader(sqlReader));
				});
			return FinalizeList(result.Select(ToCategory));
		}

		private static Category ToCategory(DbCategory db) => new()
		{
			Category_I = db.Category_I,
			Name = db.Name,
			ParentCategory_I = db.ParentCategory_I,
			Required = db.Required,
			Archived = db.Archived
		};

		private static IReadOnlyList<Category> FinalizeList(IEnumerable<Category> categories) =>
			CategoryTreeHelper.WithHasChildren(CategoryTreeHelper.SortForDisplay(categories.ToList()));
	}
}
