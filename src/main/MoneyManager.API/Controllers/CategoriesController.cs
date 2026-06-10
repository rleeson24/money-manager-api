using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Infrastructure;
using MoneyManager.Core.Application.Categories.Commands;
using MoneyManager.Core.Application.Categories.Queries;
using MoneyManager.Core.Models.Input;

namespace MoneyManager.API.Controllers
{
	/// <summary>
	/// Categories are a global resource shared across users; endpoints intentionally omit <see cref="Filters.RequireUserIdAttribute"/>.
	/// </summary>
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[Route("api/categories")]
	public class CategoriesController : ControllerBase
	{
		private readonly IMediator _mediator;

		public CategoriesController(IMediator mediator)
		{
			_mediator = mediator;
		}

		[HttpGet]
		public async Task<IActionResult> GetCategories([FromQuery] bool activeOnly = false, CancellationToken cancellationToken = default)
		{
			var categories = await _mediator.Send(new GetCategoriesQuery(activeOnly), cancellationToken);
			return Ok(categories);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetCategory(int id, CancellationToken cancellationToken = default)
		{
			var category = await _mediator.Send(new GetCategoryQuery(id), cancellationToken);
			if (category != null)
				return Ok(category);
			return ApiResults.NotFound("Category not found.");
		}

		[HttpPost]
		public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryModel model, CancellationToken cancellationToken = default)
		{
			var result = await _mediator.Send(new CreateCategoryCommand(model), cancellationToken);
			if (result.ValidationError != null)
				return ApiResults.ValidationError(result.ValidationError);
			if (result.Category != null)
				return CreatedAtAction(nameof(GetCategory), new { id = result.Category.Category_I }, result.Category);
			return ApiResults.UnexpectedFailure();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryModel model, CancellationToken cancellationToken = default)
		{
			var result = await _mediator.Send(new UpdateCategoryCommand(id, model), cancellationToken);
			if (result.ValidationError != null)
				return ApiResults.ValidationError(result.ValidationError);
			if (result.Category != null)
				return Ok(result.Category);
			if (result.IsNotFound)
				return ApiResults.NotFound("Category not found.");
			return ApiResults.UnexpectedFailure();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken = default)
		{
			var result = await _mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
			if (result.ValidationError != null)
				return ApiResults.ValidationError(result.ValidationError);
			if (result.Success)
				return NoContent();
			if (result.IsNotFound)
				return ApiResults.NotFound("Category not found.");
			return ApiResults.UnexpectedFailure();
		}
	}
}
