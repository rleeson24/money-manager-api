using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.API.Configuration;
using MoneyManager.API.Utilities;
using MoneyManager.Core.Models;
using MoneyManager.Core.UseCases.Import;

namespace MoneyManager.API.Controllers
{
	[ApiController]
	[Authorize(AuthenticationSchemes = AuthSchemes.Microsoft)]
	[Route("api/import")]
	public class ImportController : ControllerBase
	{
		private readonly IResolveUserId _resolveUserId;
		private readonly IImportFromFileUseCase _importFromFileUseCase;
		private readonly IGetLastImportDatesUseCase _getLastImportDatesUseCase;

		public ImportController(
			IResolveUserId resolveUserId,
			IImportFromFileUseCase importFromFileUseCase,
			IGetLastImportDatesUseCase getLastImportDatesUseCase)
		{
			_resolveUserId = resolveUserId;
			_importFromFileUseCase = importFromFileUseCase;
			_getLastImportDatesUseCase = getLastImportDatesUseCase;
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
			var userId = _resolveUserId.Resolve(User);
			if (userId == null)
				return Unauthorized();

			if (file == null || file.Length == 0)
				return BadRequest(new { error = "No file uploaded." });
			if (string.IsNullOrWhiteSpace(format))
				return BadRequest(new { error = "Format (OFX, QFX, or CSV) is required." });
			if (!importSource.HasValue)
				return BadRequest(new { error = "Import source is required." });
			if (!paymentMethodId.HasValue || paymentMethodId.Value <= 0)
				return BadRequest(new { error = "Payment method is required." });

			var fmt = format.Trim().ToUpperInvariant();
			if (fmt != "OFX" && fmt != "QFX" && fmt != "CSV")
				return BadRequest(new { error = "Format must be OFX, QFX, or CSV." });

			await using var stream = file.OpenReadStream();
			var result = await _importFromFileUseCase.ExecuteAsync(userId.Value, stream, format.Trim(), importSource.Value, paymentMethodId.Value, cancellationToken);
			return Ok(new { created = result.Created, skippedDuplicates = result.SkippedDuplicates, errors = result.Errors });
		}

		[HttpGet("last-import-dates")]
		public async Task<IActionResult> GetLastImportDates([FromQuery] string? paymentMethodIds)
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

			var results = await _getLastImportDatesUseCase.ExecuteAsync(userId.Value, ids);
			return Ok(results);
		}
	}
}
