using System.Text;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MoneyManager.API.Controllers;
using MoneyManager.Core.Application.Import.Commands;
using MoneyManager.Core.Application.Import.Queries;
using MoneyManager.Core.Models;

namespace MoneyManager.API.Tests.Controllers;

public class ImportControllerTests
{
	private readonly Guid _userId = ControllerTestHelper.DefaultUserId;
	private readonly Mock<IMediator> _mediator = ControllerTestHelper.CreateMediatorMock();
	private readonly Mock<ILogger<ImportController>> _logger = new();
	private readonly ImportController _controller;

	public ImportControllerTests()
	{
		_controller = ControllerTestHelper.CreateController<ImportController>(_mediator.Object, _logger.Object, _userId);
	}

	[Fact]
	public async Task ImportFile_WhenSuccessful_ReturnsOkWithResult()
	{
		var file = CreateFormFile("transactions.csv", "date,amount\n2024-01-01,10.00");
		var importResult = new ImportResult
		{
			Created = 3,
			SkippedDuplicates = 1,
			Errors = ["row 2: invalid"]
		};
		_mediator
			.Setup(m => m.Send(
				It.Is<ImportFromFileCommand>(c =>
					c.UserId == _userId &&
					c.Format == "csv" &&
					c.ImportSource == ImportSource.DiscoverChecking &&
					c.PaymentMethodId == 5),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(importResult);

		var result = await _controller.ImportFile(file, " csv ", ImportSource.DiscoverChecking, 5, CancellationToken.None);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.NotNull(ok.Value);
	}

	[Fact]
	public async Task GetLastImportDates_ReturnsOkWithResults()
	{
		var results = new List<LastImportDatesForPaymentMethod>
		{
			new() { PaymentMethodId = 1, LatestExpenseDate = new DateTime(2024, 2, 1) },
			new() { PaymentMethodId = 2, LatestDatePaid = new DateTime(2024, 2, 15) }
		};
		_mediator
			.Setup(m => m.Send(
				It.Is<GetLastImportDatesQuery>(q =>
					q.UserId == _userId &&
					q.PaymentMethodIds.Count == 2 &&
					q.PaymentMethodIds.Contains(1) &&
					q.PaymentMethodIds.Contains(2)),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(results);

		var result = await _controller.GetLastImportDates("1, 2");

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(results, ok.Value);
	}

	[Fact]
	public async Task GetLastImportDates_WhenNoIdsProvided_ReturnsOkWithEmptyFilter()
	{
		var results = Array.Empty<LastImportDatesForPaymentMethod>();
		_mediator
			.Setup(m => m.Send(
				It.Is<GetLastImportDatesQuery>(q => q.UserId == _userId && q.PaymentMethodIds.Count == 0),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(results);

		var result = await _controller.GetLastImportDates(null);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(results, ok.Value);
	}

	private static IFormFile CreateFormFile(string fileName, string content)
	{
		var bytes = Encoding.UTF8.GetBytes(content);
		var stream = new MemoryStream(bytes);
		var file = new Mock<IFormFile>();
		file.Setup(f => f.FileName).Returns(fileName);
		file.Setup(f => f.Length).Returns(bytes.Length);
		file.Setup(f => f.OpenReadStream()).Returns(stream);
		return file.Object;
	}
}
