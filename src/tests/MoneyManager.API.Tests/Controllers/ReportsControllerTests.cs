using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MoneyManager.API.Controllers;
using MoneyManager.Core.Application.Reports.Queries;
using MoneyManager.Core.Models.Reports;

namespace MoneyManager.API.Tests.Controllers;

public class ReportsControllerTests
{
	private readonly Mock<IMediator> _mediator = ControllerTestHelper.CreateMediatorMock();
	private readonly ReportsController _controller;

	public ReportsControllerTests()
	{
		_controller = ControllerTestHelper.CreateController<ReportsController>(_mediator.Object);
	}

	[Fact]
	public async Task GetBudgetReport_ReturnsOkWithReport()
	{
		var report = new BudgetReport { Month = "2026-03", TotalSpent = 12.5m };
		_mediator
			.Setup(m => m.Send(
				It.Is<GetBudgetReportQuery>(q =>
					q.UserId == ControllerTestHelper.DefaultUserId && q.Month == "2026-03"),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(report);

		var result = await _controller.GetBudgetReport("2026-03");

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(report, ok.Value);
	}

	[Fact]
	public async Task GetBudgetReport_WhenMonthMissing_SendsEmptyMonth()
	{
		var report = new BudgetReport();
		_mediator
			.Setup(m => m.Send(
				It.Is<GetBudgetReportQuery>(q => q.Month == string.Empty),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(report);

		var result = await _controller.GetBudgetReport(null);

		Assert.IsType<OkObjectResult>(result);
	}
}
