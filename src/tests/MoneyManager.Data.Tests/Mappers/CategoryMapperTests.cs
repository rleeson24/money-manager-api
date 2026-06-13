using System.Data;
using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Tests.Helpers;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Data.Tests.Mappers;

public class CategoryMapperTests : TestBase<CategoryMapper>
{
	protected DbCategory _result = null!;

	public class WithParent : CategoryMapperTests
	{
		public WithParent()
		{
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var reader = DictionaryDbDataReader.Create(new Dictionary<string, object?>
			{
				["Category_I"] = 10,
				["Name"] = "Child category",
				["ParentCategory_I"] = 5,
				["Required"] = true,
				["Archived"] = false
			});
			_result = SubjectUnderTest.FromDbReader(reader).GetAwaiter().GetResult();
		}

		[Fact]
		public void MapsCategoryId() => Assert.Equal(10, _result.Category_I);

		[Fact]
		public void MapsName() => Assert.Equal("Child category", _result.Name);

		[Fact]
		public void MapsParentCategoryId() => Assert.Equal(5, _result.ParentCategory_I);

		[Fact]
		public void MapsRequired() => Assert.True(_result.Required);

		[Fact]
		public void MapsArchived() => Assert.False(_result.Archived);
	}

	public class WithoutParent : CategoryMapperTests
	{
		public WithoutParent()
		{
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var reader = DictionaryDbDataReader.Create(new Dictionary<string, object?>
			{
				["Category_I"] = 20,
				["Name"] = "Top level",
				["ParentCategory_I"] = DBNull.Value,
				["Required"] = false,
				["Archived"] = true
			});
			_result = SubjectUnderTest.FromDbReader(reader).GetAwaiter().GetResult();
		}

		[Fact]
		public void MapsNullParentCategoryId() => Assert.Null(_result.ParentCategory_I);

		[Fact]
		public void MapsArchived() => Assert.True(_result.Archived);
	}
}
