namespace MoneyManager.API.Utilities;

public static class CsvUploadValidator
{
	private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"text/csv",
		"application/csv",
		"application/vnd.ms-excel",
		"text/plain",
		"application/octet-stream",
	};

	public static string SanitizeFileNameForLogging(string? fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return "(none)";
		}

		var name = Path.GetFileName(fileName);
		return name.Length > 200 ? name[..200] : name;
	}

	public static bool IsValidCsvUpload(IFormFile file, out string? errorMessage)
	{
		errorMessage = null;

		if (file.Length > ImportUploadLimits.MaxFileBytes)
		{
			errorMessage = "File exceeds the maximum allowed size of 10 MB.";
			return false;
		}

		if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
		{
			errorMessage = "Only .csv files are allowed.";
			return false;
		}

		if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
		{
			errorMessage = "Unsupported file content type.";
			return false;
		}

		return true;
	}
}
