using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Core.UseCases.Categories;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Route("api/categories")]
	[Authorize]
	public class CategoriesController : ControllerBase
	{
		[HttpGet]
		public async Task<IActionResult> GetCategories([FromServices] IGetCategoriesUseCase getCategoriesUseCase)
		{
			var categories = await getCategoriesUseCase.Execute();
			if (categories != null)
			{
				return Ok(categories);
			}
			return Problem();
		}
	}
}
