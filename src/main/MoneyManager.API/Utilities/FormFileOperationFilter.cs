using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MoneyManager.API.Utilities;

public sealed class FormFileOperationFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		if (operation == null) throw new ArgumentNullException(nameof(operation));
		if (context == null) throw new ArgumentNullException(nameof(context));

		var formParams = context.ApiDescription.ParameterDescriptions
			.Where(p => p.Source?.Id == "Form")
			.ToList();

		if (formParams.Count == 0)
			return;

		// Swashbuckle represents multipart form data as a requestBody, not individual parameters.
		operation.Parameters?.Clear();

		var properties = new Dictionary<string, IOpenApiSchema>();
		var schema = new OpenApiSchema
		{
			Type = JsonSchemaType.Object,
			Properties = properties,
			Required = new HashSet<string>()
		};

		foreach (var param in formParams)
		{
			var name = param.Name;
			var type = param.Type;

			IOpenApiSchema propSchema = IsFormFile(type)
				? new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" }
				: context.SchemaGenerator.GenerateSchema(type, context.SchemaRepository);

			schema.Properties[name] = propSchema;

			if (param.IsRequired)
				schema.Required.Add(name);
		}

		operation.RequestBody = new OpenApiRequestBody
		{
			Required = true,
			Content = new Dictionary<string, IOpenApiMediaType>
			{
				["multipart/form-data"] = new OpenApiMediaType { Schema = schema }
			}
		};
	}

	private static bool IsFormFile(Type type)
	{
		return type == typeof(IFormFile) ||
			   type == typeof(IFormFileCollection) ||
			   typeof(IFormFile).IsAssignableFrom(type);
	}
}

