using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MoneyManager.API.Controllers;
using MoneyManager.Core.Application.Expenses.Commands;
using MoneyManager.Core.Application.Expenses.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.API.Tests.Controllers;

public class ExpensesControllerTests
{
	private readonly Guid _userId = ControllerTestHelper.DefaultUserId;
	private readonly Mock<IMediator> _mediator = ControllerTestHelper.CreateMediatorMock();
	private readonly ExpensesController _controller;

	public ExpensesControllerTests()
	{
		_controller = ControllerTestHelper.CreateController<ExpensesController>(_mediator.Object, _userId);
	}

	[Fact]
	public async Task GetExpenses_ReturnsOkWithExpenses()
	{
		var expenses = new List<Expense> { new() { Expense_I = 1, ExpenseDescription = "Coffee" } };
		_mediator
			.Setup(m => m.Send(
				It.Is<GetExpensesQuery>(q =>
					q.UserId == _userId &&
					q.Month == "2024-01" &&
					q.PaymentMethod == 2 &&
					q.DatePaidNull == true &&
					q.Currency == "USD"),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(expenses);

		var result = await _controller.GetExpenses("2024-01", 2, true, "USD");

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(expenses, ok.Value);
	}

	[Fact]
	public async Task GetExpense_WhenFound_ReturnsOk()
	{
		var expense = new Expense { Expense_I = 4, ExpenseDescription = "Groceries" };
		_mediator
			.Setup(m => m.Send(It.Is<GetExpenseQuery>(q => q.Id == 4 && q.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expense);

		var result = await _controller.GetExpense(4);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(expense, ok.Value);
	}

	[Fact]
	public async Task GetExpense_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<GetExpenseQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Expense?)null);

		var result = await _controller.GetExpense(4);

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task CreateExpense_WhenSuccessful_ReturnsCreatedAtAction()
	{
		var model = new CreateExpenseModel { Expense = "Lunch", Amount = 12.50m };
		var expense = new Expense { Expense_I = 8, ExpenseDescription = "Lunch", Amount = 12.50m };
		_mediator
			.Setup(m => m.Send(It.Is<CreateExpenseCommand>(c => c.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(expense);

		var result = await _controller.CreateExpense(model);

		var created = Assert.IsType<CreatedAtActionResult>(result);
		Assert.Equal(nameof(ExpensesController.GetExpense), created.ActionName);
		Assert.Equal(8, created.RouteValues!["id"]);
		Assert.Same(expense, created.Value);
	}

	[Fact]
	public async Task CreateExpense_WhenUnexpectedFailure_ReturnsInternalServerError()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<CreateExpenseCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Expense?)null);

		var result = await _controller.CreateExpense(new CreateExpenseModel());

		var objectResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(500, objectResult.StatusCode);
	}

	[Fact]
	public async Task UpdateExpense_WhenSuccessful_ReturnsOk()
	{
		var expense = new Expense { Expense_I = 2, ExpenseDescription = "Updated" };
		_mediator
			.Setup(m => m.Send(It.Is<UpdateExpenseCommand>(c => c.Id == 2 && c.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(UpdateExpenseResult.Success(expense));

		var result = await _controller.UpdateExpense(2, expense);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(expense, ok.Value);
	}

	[Fact]
	public async Task UpdateExpense_WhenConflict_ReturnsConflict()
	{
		var current = new Expense { Expense_I = 2, ExpenseDescription = "Current" };
		_mediator
			.Setup(m => m.Send(It.IsAny<UpdateExpenseCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(UpdateExpenseResult.Conflict(current));

		var result = await _controller.UpdateExpense(2, new Expense { Expense_I = 2 });

		Assert.IsType<ConflictObjectResult>(result);
	}

	[Fact]
	public async Task UpdateExpense_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<UpdateExpenseCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(UpdateExpenseResult.NotFound());

		var result = await _controller.UpdateExpense(2, new Expense());

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task PatchExpense_WhenSuccessful_ReturnsOk()
	{
		var updated = new Expense { Expense_I = 6, Amount = 25m };
		_mediator
			.Setup(m => m.Send(It.Is<PatchExpenseCommand>(c => c.Id == 6 && c.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(UpdateExpenseResult.Success(updated));

		using var json = JsonDocument.Parse("""{"amount": 25}""");
		var result = await _controller.PatchExpense(6, json.RootElement);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(updated, ok.Value);
	}

	[Fact]
	public async Task PatchExpense_WhenConflict_ReturnsConflict()
	{
		var current = new Expense { Expense_I = 6, Amount = 10m };
		_mediator
			.Setup(m => m.Send(It.IsAny<PatchExpenseCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(UpdateExpenseResult.Conflict(current));

		using var json = JsonDocument.Parse("""{"amount": 25}""");
		var result = await _controller.PatchExpense(6, json.RootElement);

		Assert.IsType<ConflictObjectResult>(result);
	}

	[Fact]
	public async Task PatchExpense_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<PatchExpenseCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(UpdateExpenseResult.NotFound());

		using var json = JsonDocument.Parse("""{"amount": 25}""");
		var result = await _controller.PatchExpense(6, json.RootElement);

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task DeleteExpense_WhenSuccessful_ReturnsOk()
	{
		_mediator
			.Setup(m => m.Send(It.Is<DeleteExpenseCommand>(c => c.Id == 9 && c.UserId == _userId), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var result = await _controller.DeleteExpense(9);

		Assert.IsType<OkResult>(result);
	}

	[Fact]
	public async Task DeleteExpense_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<DeleteExpenseCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var result = await _controller.DeleteExpense(9);

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task BulkUpdateExpenses_WhenSuccessful_ReturnsOk()
	{
		var request = new BulkUpdateRequest
		{
			Ids = [1, 2],
			ExpenseDate = new DateTime(2024, 3, 1),
			Category = 5,
			DatePaid = new DateTime(2024, 3, 2)
		};
		_mediator
			.Setup(m => m.Send(
				It.Is<BulkUpdateExpensesCommand>(c =>
					c.UserId == _userId &&
					c.Request.Ids.SequenceEqual(request.Ids) &&
					c.Request.ExpenseDate == request.ExpenseDate &&
					c.Request.Category == request.Category &&
					c.Request.DatePaid == request.DatePaid),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var result = await _controller.BulkUpdateExpenses(request);

		Assert.IsType<OkResult>(result);
	}

	[Fact]
	public async Task BulkUpdateExpenses_WhenFailed_ReturnsInternalServerError()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<BulkUpdateExpensesCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var result = await _controller.BulkUpdateExpenses(new BulkUpdateRequest { Ids = [1] });

		var objectResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(500, objectResult.StatusCode);
	}

	[Fact]
	public async Task BulkDeleteExpenses_WhenSuccessful_ReturnsOk()
	{
		var request = new BulkDeleteRequest { Ids = [3, 4] };
		_mediator
			.Setup(m => m.Send(
				It.Is<BulkDeleteExpensesCommand>(c => c.UserId == _userId && c.Ids.SequenceEqual(request.Ids)),
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		var result = await _controller.BulkDeleteExpenses(request);

		Assert.IsType<OkResult>(result);
	}

	[Fact]
	public async Task BulkDeleteExpenses_WhenFailed_ReturnsInternalServerError()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<BulkDeleteExpensesCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		var result = await _controller.BulkDeleteExpenses(new BulkDeleteRequest { Ids = [3] });

		var objectResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(500, objectResult.StatusCode);
	}
}
