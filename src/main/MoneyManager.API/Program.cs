using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MoneyManager.API.Configuration;
using MoneyManager.API.Health;
using MoneyManager.API.Middleware;
using MoneyManager.API.Utilities;
using FluentValidation;
using MoneyManager.Core;
using MoneyManager.Data;
using MoneyManager.Import;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddEnvironmentVariables()
	.AddUserSecrets<Program>(optional: true, reloadOnChange: true);

string? keyVaultName = builder.Configuration["KeyVaultName"];
if (!string.IsNullOrEmpty(keyVaultName))
{
	var secretClient = new SecretClient(
		new Uri($"https://{keyVaultName}.vault.azure.net/"),
		new DefaultAzureCredential());
	builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
}

builder.AddServiceDefaults();
builder.AddDefaultRateLimiting();
builder.AddSqlServerClient("DefaultConnection", settings => settings.DisableHealthChecks = true);
builder.AddMoneyManagerHealthChecks();
var services = builder.Services;

builder.WebHost.ConfigureKestrel(options =>
{
	options.Limits.MaxRequestBodySize = ImportUploadLimits.MaxFileBytes;
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
	options.AddPolicy("Default", policy =>
	{
		if (allowedOrigins.Length > 0)
		{
			policy.WithOrigins(allowedOrigins)
				  .AllowAnyMethod()
				  .AllowAnyHeader()
				  .AllowCredentials();
		}
		else if (builder.Environment.IsEnvironment("Local") || builder.Environment.IsDevelopment())
		{
			// Development/local: be permissive (do not allow credentials with AllowAnyOrigin)
			policy.AllowAnyOrigin()
				  .AllowAnyMethod()
				  .AllowAnyHeader();
		}
		else
		{
			throw new InvalidOperationException("CORS: 'AllowedOrigins' is not configured for non-local environment.");
		}
	});
});

// Azure AD is the sole authentication scheme. Do not register a second JwtBearer
// handler as the default — it will intercept Azure AD tokens and reject their issuer.
builder.Services.AddAuthentication(AuthSchemes.Microsoft)
	.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), AuthSchemes.Microsoft);

builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, AzureAdJwtBearerPostConfigure>();

builder.Services.AddControllers()
	.AddJsonOptions(opts => 
		opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Minimal APIs use HttpJsonOptions for serialization
builder.Services.ConfigureHttpJsonOptions(opts =>
	opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.OperationFilter<FormFileOperationFilter>();
});
builder.Services.AddHttpClient();

// OpenTelemetry (including logs/traces/metrics export to Application Insights) is configured
// via AddServiceDefaults() in MoneyManager.ServiceDefaults.
builder.Logging.AddAzureWebAppDiagnostics();
builder.Logging.AddConsole();

builder.Services.AddAuthorization(options =>
{
	options.FallbackPolicy = new AuthorizationPolicyBuilder()
		.RequireAuthenticatedUser()
		.AddAuthenticationSchemes(AuthSchemes.Microsoft)
		.Build();
});

services.AddScoped<IResolveUserId, ResolveUserId>();
services.AddScoped<MoneyManager.API.Filters.RequireUserIdFilter>();
services.AddCoreServices();
services.AddDataServices(builder.Configuration);
builder.Services.AddImportParsers();

var detailedErrorsValue = builder.Configuration.GetValue<bool>("DetailedErrors");
var app = builder.Build();

// Startup log
app.Logger.LogInformation("=== Application starting ===");
app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
app.Logger.LogInformation("DetailedErrors flag: {Flag}", app.Configuration.GetValue<bool>("DetailedErrors"));
app.Logger.LogInformation("Allowed origins: {Origins}", string.Join(", ", allowedOrigins));
var appInsightsConfigured = !string.IsNullOrWhiteSpace(
	app.Configuration["ApplicationInsights:ConnectionString"]
	?? app.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]);
app.Logger.LogInformation("Application Insights export: {Status}", appInsightsConfigured ? "enabled" : "disabled");
AzureAdConfigurationValidator.LogConfigurationStatus(app.Configuration, app.Logger);

app.UseExceptionHandler(exceptionHandlerApp =>
{
	exceptionHandlerApp.Run(async context =>
	{
		var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
		var exception = exceptionHandlerFeature?.Error;

		if (exception is ValidationException validationException)
		{
			context.Response.StatusCode = StatusCodes.Status400BadRequest;
			context.Response.ContentType = "application/problem+json";
			var errors = validationException.Errors
				.GroupBy(e => e.PropertyName)
				.ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
			await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(errors)
			{
				Status = StatusCodes.Status400BadRequest,
				Title = "Validation failed",
				Detail = "One or more validation errors occurred."
			});
			return;
		}

		if (exception != null)
		{
			var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
			logger.LogError(exception, "Unhandled exception: {Message} | Path: {Path} | TraceId: {TraceId}",
				exception.Message, context.Request.Path, Activity.Current?.Id ?? context.TraceIdentifier);
		}

		context.Response.StatusCode = StatusCodes.Status500InternalServerError;
		context.Response.ContentType = "application/json";

		bool showDetails = app.Environment.IsDevelopment() ||
						   app.Configuration.GetValue<bool>("DetailedErrors") ||
						   app.Configuration.GetValue<bool>("ASPNETCORE_DETAILEDERRORS");

		object response = showDetails && exception != null
			? new
			{
				error = "Internal Server Error",
				message = exception.Message,
				type = exception.GetType().FullName,
				stackTrace = exception.StackTrace,
				source = exception.Source,
				innerException = exception.InnerException?.Message,
				traceId = Activity.Current?.Id ?? context.TraceIdentifier
			}
			: new
			{
				type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
				title = "An error occurred while processing your request.",
				status = 500,
				detail = "An unexpected error occurred",
				traceId = Activity.Current?.Id ?? context.TraceIdentifier
			};

		await context.Response.WriteAsJsonAsync(response, new JsonSerializerOptions { WriteIndented = true });
	});
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}
else if (!app.Environment.IsEnvironment("Local"))
{
	app.UseHsts();
	app.UseHttpsRedirection();
}

app.UseCors("Default");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

app.MapControllers().RequireRateLimiting("api");

app.Run();
