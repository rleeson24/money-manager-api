using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Filters;
using MoneyManager.API.Infrastructure;
using MoneyManager.Core.Application.Import.Commands;
using MoneyManager.Core.Application.Import.Queries;
using MoneyManager.Core.Models;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[RequireUserId]
	[Route("api/import")]
	public class ImportController : ControllerBase
	{
		private readonly IMediator _mediator;
		private readonly ILogger<ImportController> _logger;

		public ImportController(IMediator mediator, ILogger<ImportController> logger)
		{
			_mediator = mediator;
			_logger = logger;
		}

		[HttpPost("file")]
		[RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
		public async Task<IActionResult> ImportFile(
			[FromForm] IFormFile? file,
			[FromForm] string? format,
			[FromForm] ImportSource? importSource,
			[FromForm] int? paymentMethodId,
			CancellationToken cancellationToken)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);

			_logger.LogInformation(
				"Import file request received: file={FileName}, size={FileSize}, format={Format}, source={ImportSource}, paymentMethod={PaymentMethodId}",
				file?.FileName,
				file?.Length,
				format,
				importSource,
				paymentMethodId);

			if (file == null || file.Length == 0)
			{
				_logger.LogWarning("Import file request rejected for user {UserId}: no file uploaded", userId);
				return ApiResults.ValidationError("No file uploaded.");
			}

			_logger.LogInformation(
				"Import file request accepted for user {UserId}: {FileName} ({FileSize} bytes), format={Format}, source={ImportSource}, paymentMethod={PaymentMethodId}",
				userId, file.FileName, file.Length, format, importSource, paymentMethodId);

			await using var stream = file.OpenReadStream();
			var result = await _mediator.Send(
				new ImportFromFileCommand(
					userId,
					stream,
					format?.Trim() ?? string.Empty,
					importSource,
					paymentMethodId ?? 0),
				cancellationToken);

			_logger.LogInformation(
				"Import file completed for user {UserId}: created={Created}, skippedDuplicates={SkippedDuplicates}, errors={ErrorCount}",
				userId,
				result.Created,
				result.SkippedDuplicates,
				result.Errors.Count);

			return Ok(new { created = result.Created, skippedDuplicates = result.SkippedDuplicates, errors = result.Errors });
		}

		[HttpGet("last-import-dates")]
		public async Task<IActionResult> GetLastImportDates([FromQuery] string? paymentMethodIds, CancellationToken cancellationToken = default)
		{
			var userId = UserIdHttpContext.GetRequired(HttpContext);

			var ids = new List<int>();
			if (!string.IsNullOrWhiteSpace(paymentMethodIds))
			{
				foreach (var part in paymentMethodIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if (int.TryParse(part, out var id))
						ids.Add(id);
				}
			}

			var results = await _mediator.Send(new GetLastImportDatesQuery(userId, ids), cancellationToken);
			return Ok(results);
		}
	}
}
