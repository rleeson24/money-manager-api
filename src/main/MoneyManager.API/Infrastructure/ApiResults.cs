using Microsoft.AspNetCore.Mvc;

namespace MoneyManager.API.Infrastructure
{
	public static class ApiResults
	{
		public static IActionResult ValidationError(string detail) =>
			new BadRequestObjectResult(new ProblemDetails
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Validation failed",
				Detail = detail
			});

		public static IActionResult ValidationError(IDictionary<string, string[]> errors) =>
			new BadRequestObjectResult(new ValidationProblemDetails(errors)
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Validation failed"
			});

		public static IActionResult NotFound(string detail) =>
			new NotFoundObjectResult(new ProblemDetails
			{
				Status = StatusCodes.Status404NotFound,
				Title = "Not found",
				Detail = detail
			});

		public static IActionResult Conflict(string detail, object? extensions = null)
		{
			var problem = new ProblemDetails
			{
				Status = StatusCodes.Status409Conflict,
				Title = "Conflict",
				Detail = detail
			};
			if (extensions != null)
				problem.Extensions["conflict"] = extensions;
			return new ConflictObjectResult(problem);
		}

		public static IActionResult UnexpectedFailure(string? detail = null) =>
			new ObjectResult(new ProblemDetails
			{
				Status = StatusCodes.Status500InternalServerError,
				Title = "An error occurred while processing your request.",
				Detail = detail ?? "An unexpected error occurred."
			})
			{ StatusCode = StatusCodes.Status500InternalServerError };
	}
}
