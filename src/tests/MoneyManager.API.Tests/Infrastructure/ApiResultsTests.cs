using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Infrastructure;

namespace MoneyManager.API.Tests.Infrastructure;

public class ApiResultsTests
{
	[Fact]
	public void ValidationError_WithString_ReturnsBadRequest()
	{
		var result = ApiResults.ValidationError("Invalid input.");

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
		var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
		Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
		Assert.Equal("Validation failed", problem.Title);
		Assert.Equal("Invalid input.", problem.Detail);
	}

	[Fact]
	public void ValidationError_WithDictionary_ReturnsBadRequest()
	{
		var errors = new Dictionary<string, string[]>
		{
			["Name"] = ["Name is required."]
		};

		var result = ApiResults.ValidationError(errors);

		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
		var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
		Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
		Assert.Equal("Validation failed", problem.Title);
		Assert.Equal("Name is required.", problem.Errors["Name"][0]);
	}

	[Fact]
	public void NotFound_ReturnsNotFound()
	{
		var result = ApiResults.NotFound("Entity not found.");

		var notFound = Assert.IsType<NotFoundObjectResult>(result);
		Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
		var problem = Assert.IsType<ProblemDetails>(notFound.Value);
		Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
		Assert.Equal("Not found", problem.Title);
		Assert.Equal("Entity not found.", problem.Detail);
	}

	[Fact]
	public void Conflict_ReturnsConflict()
	{
		var extensions = new { id = 42 };

		var result = ApiResults.Conflict("Conflict occurred.", extensions);

		var conflict = Assert.IsType<ConflictObjectResult>(result);
		Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
		var problem = Assert.IsType<ProblemDetails>(conflict.Value);
		Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
		Assert.Equal("Conflict", problem.Title);
		Assert.Equal("Conflict occurred.", problem.Detail);
		Assert.True(problem.Extensions.ContainsKey("conflict"));
	}

	[Fact]
	public void Conflict_WithoutExtensions_ReturnsConflict()
	{
		var result = ApiResults.Conflict("Conflict occurred.");

		var conflict = Assert.IsType<ConflictObjectResult>(result);
		Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
	}

	[Fact]
	public void Unauthorized_ReturnsUnauthorized()
	{
		var result = ApiResults.Unauthorized();

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
		Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
		var problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
		Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
		Assert.Equal("Unauthorized", problem.Title);
		Assert.Equal("User identity could not be resolved.", problem.Detail);
	}

	[Fact]
	public void Unauthorized_WithCustomDetail_ReturnsUnauthorized()
	{
		var result = ApiResults.Unauthorized("Custom unauthorized message.");

		var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
		var problem = Assert.IsType<ProblemDetails>(unauthorized.Value);
		Assert.Equal("Custom unauthorized message.", problem.Detail);
	}

	[Fact]
	public void UnexpectedFailure_ReturnsInternalServerError()
	{
		var result = ApiResults.UnexpectedFailure();

		var objectResult = Assert.IsType<ObjectResult>(result);
		Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
		var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
		Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
		Assert.Equal("An error occurred while processing your request.", problem.Title);
		Assert.Equal("An unexpected error occurred.", problem.Detail);
	}

	[Fact]
	public void UnexpectedFailure_WithCustomDetail_ReturnsInternalServerError()
	{
		var result = ApiResults.UnexpectedFailure("Something went wrong.");

		var objectResult = Assert.IsType<ObjectResult>(result);
		var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
		Assert.Equal("Something went wrong.", problem.Detail);
	}
}
