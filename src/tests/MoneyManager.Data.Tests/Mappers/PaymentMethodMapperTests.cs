using MoneyManager.Data.Mappers;
using MoneyManager.Data.Models;
using MoneyManager.Data.Tests.Helpers;
using MoneyManager.Tests.Utilities;
using Xunit;

namespace MoneyManager.Data.Tests.Mappers;

public class PaymentMethodMapperTests : TestBase<PaymentMethodMapper>
{
	protected DbPaymentMethod _result = null!;

	public class FromDbReader : PaymentMethodMapperTests
	{
		public FromDbReader()
		{
			BuildSubject();
			ExecuteTestMethod();
		}

		protected override void ExecuteTestMethod()
		{
			var reader = DictionaryDbDataReader.Create(new Dictionary<string, object?>
			{
				["ID"] = 3,
				["PaymentMethod"] = "Discover Credit"
			});
			_result = SubjectUnderTest.FromDbReader(reader).GetAwaiter().GetResult();
		}

		[Fact]
		public void MapsId() => Assert.Equal(3, _result.ID);

		[Fact]
		public void MapsPaymentMethodName() => Assert.Equal("Discover Credit", _result.PaymentMethod);
	}
}
