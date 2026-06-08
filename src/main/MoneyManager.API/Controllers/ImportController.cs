using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Utilities;
using MoneyManager.Core.Application.Import.Commands;
using MoneyManager.Core.Constants;
using MoneyManager.Core.Application.Import.Queries;
using MoneyManager.Core.Models;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[Route("api/import")]
	public class ImportController : ControllerBase
	{
		private readonly IResolveUserId _resolveUserId;
		private readonly IMediator _mediator;
		private readonly ILogger<ImportController> _logger;

		public ImportController(
			IResolveUserId resolveUserId,
			IMediator mediator,
			ILogger<ImportController> logger)
		{
			_resolveUserId = resolveUserId;
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
			_logger.LogInformation(
				"Import file request received: file={FileName}, size={FileSize}, format={Format}, source={ImportSource}, paymentMethod={PaymentMethodId}",
				file?.FileName,
				file?.Length,
				format,
				importSource,
				paymentMethodId);

			var userId = _resolveUserId.Resolve(User);
			if (userId == null)
			{
				_logger.LogWarning("Import file request rejected: unauthorized");
				return Unauthorized();
			}

			if (file == null || file.Length == 0)
			{
				_logger.LogWarning("Import file request rejected for user {UserId}: no file uploaded", userId);
				return BadRequest(new { error = "No file uploaded." });
			}
			if (string.IsNullOrWhiteSpace(format))
			{
				_logger.LogWarning("Import file request rejected for user {UserId}: format missing", userId);
				return BadRequest(new { error = "Format (CSV) is required." });
			}
			if (!importSource.HasValue)
			{
				_logger.LogWarning("Import file request rejected for user {UserId}: import source missing", userId);
				return BadRequest(new { error = "Import source is required." });
			}
			if (!paymentMethodId.HasValue || paymentMethodId.Value <= 0)
			{
				_logger.LogWarning("Import file request rejected for user {UserId}: payment method missing", userId);
				return BadRequest(new { error = "Payment method is required." });
			}

			var fmt = format.Trim().ToUpperInvariant();

			if (!ImportFormat.IsCsv(fmt))
			{
				_logger.LogWarning(
					"Import file request rejected for user {UserId}: invalid format {Format}",
					userId,
					fmt);
				return BadRequest(new { error = "Format must be CSV." });
			}

			_logger.LogInformation(
				"Import file request accepted for user {UserId}: {FileName} ({FileSize} bytes), format={Format}, source={ImportSource}, paymentMethod={PaymentMethodId}",
				userId, file.FileName, file.Length, fmt, importSource, paymentMethodId);

			await using var stream = file.OpenReadStream();
			var result = await _mediator.Send(
				new ImportFromFileCommand(userId.Value, stream, format.Trim(), importSource.Value, paymentMethodId.Value),
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
			var userId = _resolveUserId.Resolve(User);
			if (userId == null)
				return Unauthorized();

			var ids = new List<int>();
			if (!string.IsNullOrWhiteSpace(paymentMethodIds))
			{
				foreach (var part in paymentMethodIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					if (int.TryParse(part, out var id))
						ids.Add(id);
				}
			}

			var results = await _mediator.Send(new GetLastImportDatesQuery(userId.Value, ids), cancellationToken);
			return Ok(results);
		}
	}
}
