using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MoneyManager.API.Controllers;
using MoneyManager.Core.Application.Categories.Commands;
using MoneyManager.Core.Application.Categories.Queries;
using MoneyManager.Core.Models;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.Repositories;

namespace MoneyManager.API.Tests.Controllers;

public class CategoriesControllerTests
{
	private readonly Mock<IMediator> _mediator = ControllerTestHelper.CreateMediatorMock();
	private readonly CategoriesController _controller;

	public CategoriesControllerTests()
	{
		_controller = ControllerTestHelper.CreateController<CategoriesController>(_mediator.Object);
	}

	[Fact]
	public async Task GetCategories_ReturnsOkWithCategories()
	{
		var categories = new List<Category> { new() { Category_I = 1, Name = "Food" } };
		_mediator
			.Setup(m => m.Send(It.Is<GetCategoriesQuery>(q => q.ActiveOnly == true), It.IsAny<CancellationToken>()))
			.ReturnsAsync(categories);

		var result = await _controller.GetCategories(activeOnly: true);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(categories, ok.Value);
	}

	[Fact]
	public async Task GetCategory_WhenFound_ReturnsOk()
	{
		var category = new Category { Category_I = 5, Name = "Travel" };
		_mediator
			.Setup(m => m.Send(It.Is<GetCategoryQuery>(q => q.Id == 5), It.IsAny<CancellationToken>()))
			.ReturnsAsync(category);

		var result = await _controller.GetCategory(5);

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(category, ok.Value);
	}

	[Fact]
	public async Task GetCategory_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<GetCategoryQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Category?)null);

		var result = await _controller.GetCategory(99);

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task CreateCategory_WhenSuccessful_ReturnsCreatedAtAction()
	{
		var model = new CreateCategoryModel { Name = "Utilities" };
		var category = new Category { Category_I = 10, Name = "Utilities" };
		_mediator
			.Setup(m => m.Send(It.IsAny<CreateCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryMutationResult.Success(category));

		var result = await _controller.CreateCategory(model);

		var created = Assert.IsType<CreatedAtActionResult>(result);
		Assert.Equal(nameof(CategoriesController.GetCategory), created.ActionName);
		Assert.Equal(10, created.RouteValues!["id"]);
		Assert.Same(category, created.Value);
	}

	[Fact]
	public async Task CreateCategory_WhenValidationError_ReturnsBadRequest()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<CreateCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryMutationResult.Error("Name is required."));

		var result = await _controller.CreateCategory(new CreateCategoryModel());

		Assert.IsType<BadRequestObjectResult>(result);
	}

	[Fact]
	public async Task CreateCategory_WhenUnexpectedFailure_ReturnsInternalServerError()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<CreateCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CategoryMutationResult());

		var result = await _controller.CreateCategory(new CreateCategoryModel { Name = "Other" });

		var objectResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(500, objectResult.StatusCode);
	}

	[Fact]
	public async Task UpdateCategory_WhenSuccessful_ReturnsOk()
	{
		var category = new Category { Category_I = 3, Name = "Updated" };
		_mediator
			.Setup(m => m.Send(It.Is<UpdateCategoryCommand>(c => c.Id == 3), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryMutationResult.Success(category));

		var result = await _controller.UpdateCategory(3, new UpdateCategoryModel { Name = "Updated" });

		var ok = Assert.IsType<OkObjectResult>(result);
		Assert.Same(category, ok.Value);
	}

	[Fact]
	public async Task UpdateCategory_WhenValidationError_ReturnsBadRequest()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<UpdateCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryMutationResult.Error("Invalid parent."));

		var result = await _controller.UpdateCategory(3, new UpdateCategoryModel());

		Assert.IsType<BadRequestObjectResult>(result);
	}

	[Fact]
	public async Task UpdateCategory_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<UpdateCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryMutationResult.NotFound());

		var result = await _controller.UpdateCategory(3, new UpdateCategoryModel { Name = "Missing" });

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task UpdateCategory_WhenNotFoundResult_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<UpdateCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryMutationResult.NotFound());

		var result = await _controller.UpdateCategory(3, new UpdateCategoryModel { Name = "X" });

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task DeleteCategory_WhenSuccessful_ReturnsNoContent()
	{
		_mediator
			.Setup(m => m.Send(It.Is<DeleteCategoryCommand>(c => c.Id == 7), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryDeleteResult.Ok());

		var result = await _controller.DeleteCategory(7);

		Assert.IsType<NoContentResult>(result);
	}

	[Fact]
	public async Task DeleteCategory_WhenValidationError_ReturnsBadRequest()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<DeleteCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryDeleteResult.Error("Category has children."));

		var result = await _controller.DeleteCategory(7);

		Assert.IsType<BadRequestObjectResult>(result);
	}

	[Fact]
	public async Task DeleteCategory_WhenNotFound_ReturnsNotFound()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<DeleteCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CategoryDeleteResult.NotFound());

		var result = await _controller.DeleteCategory(7);

		Assert.IsType<NotFoundObjectResult>(result);
	}

	[Fact]
	public async Task DeleteCategory_WhenUnexpectedFailure_ReturnsInternalServerError()
	{
		_mediator
			.Setup(m => m.Send(It.IsAny<DeleteCategoryCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CategoryDeleteResult());

		var result = await _controller.DeleteCategory(7);

		var objectResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(500, objectResult.StatusCode);
	}
}
