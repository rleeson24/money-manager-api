using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Utilities;

namespace MoneyManager.Data.Repositories
{
	public class CategoryRepository : ICategoryRepository
	{
		private readonly DbExecutor _db;
		private readonly ICategoryMapper _mapper;

		public CategoryRepository(DbExecutor db, ICategoryMapper mapper)
		{
			_db = db;
			_mapper = mapper;
		}

		public async Task<IEnumerable<DbCategory>> GetAll()
		{
			var result = new List<DbCategory>();
			await _db.ExecuteReader("SELECT * FROM Categories ORDER BY Name", [],
				async sqlReader =>
				{
					while (await sqlReader.ReadAsync())
					{
						result.Add(await _mapper.FromDbReader(sqlReader));
					}
				});
			return result;
		}
	}
}
