using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MoneyManager.API.Controllers;
using MoneyManager.Core.Application.ExpenseSplits.Commands;
using MoneyManager.Core.Application.ExpenseSplits.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.API.Tests.Controllers;

public class ExpenseSplitsControllerTests
{
	private readonly Guid _userId = ControllerTestHelper.DefaultUserId;
	private readonly Mock<IMediator> _mediator = ControllerTestHelper.CreateMediatorMock();
	private readonly ExpenseSplitsController _controller;

	public ExpenseSplitsControllerTests()
	{
		_controller = ControllerTestHelper.CreateController<ExpenseSplitsController>(_mediator.Object, _userId);
	}

	[Fact]
	public async Task GetExpenseSplits_ReturnsOkWithSplits()
	{
		var splits = new List<ExpenseSplit> { new() { Id = 1, Expense_I = 10, Description = "Part A", Amount = 5m, Category = 2 } };
		_mediator
			.Setup(m => m.Send(It.Is<GetExpenseSplitsQuery>(q => q.ExpenseId == 10 && q.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(splits);

		var result = await _controller.GetExpenseSplits(10);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(splits, ok.Value);
	}

	[Fact]
	public async Task CreateExpenseSplit_WhenSuccessful_ReturnsCreatedAtAction()
	{
		var model = new CreateOrUpdateExpenseSplitModel { Expense_I = 10, Description = "Split", Amount = 15m, Category = 3 };
		var split = new ExpenseSplit { Id = 7, Expense_I = 10, Description = "Split", Amount = 15m, Category = 3 };
		_mediator
			.Setup(m => m.Send(It.Is<CreateExpenseSplitCommand>(c => c.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(split);

		var result = await _controller.CreateExpenseSplit(model);

		var created = Assert.IsType<CreatedAtActionResult>(result);
		Assert.Equal(nameof(ExpenseSplitsController.GetExpenseSplits), created.ActionName);
		Assert.Equal(10, created.RouteValues!["expenseId"]);
		Assert.Same(split, created.Value);
	}

	[Fact]
	public async Task CreateExpenseSplit_WhenExpenseNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<CreateExpenseSplitCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ExpenseSplit?)null);

		var result = await _controller.CreateExpenseSplit(new CreateOrUpdateExpenseSplitModel { Expense_I = 10 });

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task UpdateExpenseSplit_WhenSuccessful_ReturnsOk()
	{
		var model = new CreateOrUpdateExpenseSplitModel { Expense_I = 10, Description = "Updated", Amount = 20m, Category = 4 };
		var split = new ExpenseSplit { Id = 11, Expense_I = 10, Description = "Updated", Amount = 20m, Category = 4 };
		_mediator
			.Setup(m => m.Send(It.Is<UpdateExpenseSplitCommand>(c => c.Id == 11 && c.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(split);

		var result = await _controller.UpdateExpenseSplit(11, model);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(split, ok.Value);
	}

	[Fact]
	public async Task UpdateExpenseSplit_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<UpdateExpenseSplitCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ExpenseSplit?)null);

		var result = await _controller.UpdateExpenseSplit(11, new CreateOrUpdateExpenseSplitModel { Expense_I = 10 });

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task DeleteExpenseSplit_WhenSuccessful_ReturnsNoContent()
	{
		_mediator
			.Setup(m => m.Send(It.Is<DeleteExpenseSplitCommand>(c => c.Id == 12 && c.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var result = await _controller.DeleteExpenseSplit(12);

		Assert.IsType<NoContentResult>(result);
	}

	[Fact]
	public async Task DeleteExpenseSplit_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<DeleteExpenseSplitCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var result = await _controller.DeleteExpenseSplit(12);

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task ReplaceExpenseSplits_WhenSuccessful_ReturnsOk()
	{
		var request = new ReplaceExpenseSplitsRequest();
		var splits = new List<ExpenseSplit> { new() { Id = 1, Expense_I = 20, Description = "A", Amount = 10m, Category = 1 } };
		_mediator
			.Setup(m => m.Send(It.Is<ReplaceExpenseSplitsCommand>(c => c.ExpenseId == 20 && c.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ReplaceSplitsResult.Success(splits));

		var result = await _controller.ReplaceExpenseSplits(20, request);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(splits, ok.Value);
	}

	[Fact]
	public async Task ReplaceExpenseSplits_WhenExpenseNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<ReplaceExpenseSplitsCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ReplaceSplitsResult.Failure("Expense not found."));

		var result = await _controller.ReplaceExpenseSplits(20, new ReplaceExpenseSplitsRequest());

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task ReplaceExpenseSplits_WhenValidationError_ReturnsBadRequest()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<ReplaceExpenseSplitsCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(ReplaceSplitsResult.Failure("Splits must sum to expense amount."));

		var result = await _controller.ReplaceExpenseSplits(20, new ReplaceExpenseSplitsRequest());

		Assert.IsType<BadRequestObjectResult>(result);
	}
}
