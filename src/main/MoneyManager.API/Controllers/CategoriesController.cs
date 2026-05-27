using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.Core.Models.Input;
using MoneyManager.Core.UseCases.Categories;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[Route("api/categories")]
	public class CategoriesController : ControllerBase
	{
		[HttpGet]
		public async Task<IActionResult> GetCategories(
			[FromServices] IGetCategoriesUseCase getCategoriesUseCase,
			[FromQuery] bool activeOnly = false)
		{
			var categories = await getCategoriesUseCase.Execute(activeOnly);
			if (categories != null)
				return Ok(categories);
			return Problem();
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetCategory(
			[FromServices] IGetCategoryUseCase getCategoryUseCase,
			int id)
		{
			var category = await getCategoryUseCase.Execute(id);
			if (category != null)
				return Ok(category);
			return NotFound();
		}

		[HttpPost]
		public async Task<IActionResult> CreateCategory(
			[FromServices] ICreateCategoryUseCase createCategoryUseCase,
			[FromBody] CreateCategoryModel model)
		{
			var result = await createCategoryUseCase.Execute(model);
			if (result.ValidationError != null)
				return BadRequest(new { error = result.ValidationError });
			if (result.Category != null)
				return CreatedAtAction(nameof(GetCategory), new { id = result.Category.Category_I }, result.Category);
			return Problem();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateCategory(
			[FromServices] IUpdateCategoryUseCase updateCategoryUseCase,
			int id,
			[FromBody] UpdateCategoryModel model)
		{
			var result = await updateCategoryUseCase.Execute(id, model);
			if (result.ValidationError != null)
				return BadRequest(new { error = result.ValidationError });
			if (result.Category != null)
				return Ok(result.Category);
			if (result.IsNotFound)
				return NotFound();
			return Problem();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteCategory(
			[FromServices] IDeleteCategoryUseCase deleteCategoryUseCase,
			int id)
		{
			var result = await deleteCategoryUseCase.Execute(id);
			if (result.ValidationError != null)
				return BadRequest(new { error = result.ValidationError });
			if (result.Success)
				return NoContent();
			if (result.IsNotFound)
				return NotFound();
			return Problem();
		}
	}
}
