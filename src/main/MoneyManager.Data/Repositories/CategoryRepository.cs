using System.Collections.Generic;
using System.Linq;
using MoneyManager.Core.Models;
using MoneyManager.Core.Repositories;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;

namespace MoneyManager.Data.Repositories
{
	public class CategoryRepository : ICategoryRepository
	{
		private readonly DbExecutor _db;
		private readonly ICategoryMapper _readerMapper;

		public CategoryRepository(DbExecutor db, ICategoryMapper readerMapper)
		{
			_db = db;
			_readerMapper = readerMapper;
		}

		public async Task<IReadOnlyList<Category>> GetAll()
		{
			var result = new List<DbCategory>();
			await _db.ExecuteReader("SELECT * FROM Categories ORDER BY Name", [],
				async sqlReader =>
				{
					while (await sqlReader.ReadAsync())
						result.Add(await _readerMapper.FromDbReader(sqlReader));
				});
			return result.Select(db => new Category
			{
				Category_I = db.Category_I,
				Name = db.Name
			}).ToList();
		}
	}
}
