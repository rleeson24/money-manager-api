using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MoneyManager.API.Utilities;
using MoneyManager.Core;
using MoneyManager.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var services = builder.Services;

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

builder.WebHost.ConfigureKestrel(options =>
{
	options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100 MB
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
	options.AddPolicy("Default",
		builder => builder
			.WithOrigins(allowedOrigins)
			.AllowAnyMethod()
			.AllowAnyHeader()
			.AllowCredentials()
		);
});

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = false,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["JwtToken:Issuer"],
			ValidAudience = builder.Configuration["JwtToken:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["JwtToken:SecretKey"] ?? throw new InvalidOperationException("JwtToken:SecretKey not configured")))
		};
	})
	.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"), "Microsoft")
	.EnableTokenAcquisitionToCallDownstreamApi()
	.AddMicrosoftGraph(builder.Configuration.GetSection("Graph"))
	.AddInMemoryTokenCaches();

builder.Services.AddControllers()
	.AddJsonOptions(opts => 
		opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Logging
builder.Services.AddApplicationInsightsTelemetry(options =>
{
	options.EnableAdaptiveSampling = false;     // 100% sampling
	options.EnableDebugLogger = true;
});

//builder.Logging.AddApplicationInsights(
//	configureTelemetryConfiguration: cfg => cfg.ConnectionString =
//		builder.Configuration["ApplicationInsights:ConnectionString"] ??
//		builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"],
//	configureApplicationInsightsLoggerOptions: o => o.IncludeScopes = true);

builder.Logging.AddAzureWebAppDiagnostics();
builder.Logging.AddConsole();

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("ApiScope", p => p.RequireAuthenticatedUser());
});

services.AddScoped<IResolveUserId, ResolveUserId>();
services.AddCoreServices(builder.Configuration);
services.AddDataServices(builder.Configuration);

var detailedErrorsValue = builder.Configuration.GetValue<bool>("DetailedErrors");
var app = builder.Build();

app.MapDefaultEndpoints();

// Startup log
app.Logger.LogInformation("=== Application starting ===");
app.Logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);
app.Logger.LogInformation("DetailedErrors flag: {Flag}", app.Configuration.GetValue<bool>("DetailedErrors"));
app.Logger.LogInformation("Allowed origins: {Origins}", string.Join(", ", allowedOrigins));

app.UseExceptionHandler(exceptionHandlerApp =>
{
	exceptionHandlerApp.Run(async context =>
	{
		var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
		var exception = exceptionHandlerFeature?.Error;

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

app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
