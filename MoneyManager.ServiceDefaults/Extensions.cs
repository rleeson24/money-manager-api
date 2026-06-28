using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string ReadinessEndpointPath = "/health/ready";
    private const string AlivenessEndpointPath = "/alive";
    private const string LivenessEndpointPath = "/health/live";
    private const string DatabaseEndpointPath = "/health/db";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: builder.Environment.ApplicationName,
                    serviceVersion: typeof(Extensions).Assembly.GetName().Version?.ToString() ?? "unknown")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = builder.Environment.EnvironmentName
                }))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        // Exclude health check requests from tracing
                        options.Filter = context => !IsHealthCheckPath(context.Request.Path))
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    });
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        var applicationInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"]
            ?? builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        if (!string.IsNullOrWhiteSpace(applicationInsightsConnectionString))
        {
            builder.Services.AddOpenTelemetry()
                .UseAzureMonitor(options => options.ConnectionString = applicationInsightsConnectionString);
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Process is running; safe for frequent probes (does not touch dependencies).
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live", "ready"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Health endpoints are anonymous so platform probes work without Azure AD tokens.
        // Point load balancers and App Service at /alive or /health/live so auto-pause SQL is not woken on every probe.
        // Use /health/db when you explicitly want to verify database connectivity (that call will wake a paused database).
        var readinessOptions = new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready")
        };
        var livenessOptions = new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live")
        };
        var databaseOptions = new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("db")
        };

        app.MapHealthChecks(HealthEndpointPath, readinessOptions).AllowAnonymous();
        app.MapHealthChecks(ReadinessEndpointPath, readinessOptions).AllowAnonymous();
        app.MapHealthChecks(AlivenessEndpointPath, livenessOptions).AllowAnonymous();
        app.MapHealthChecks(LivenessEndpointPath, livenessOptions).AllowAnonymous();
        app.MapHealthChecks(DatabaseEndpointPath, databaseOptions).AllowAnonymous();

        return app;
    }

    private static bool IsHealthCheckPath(PathString path) =>
        path.StartsWithSegments(HealthEndpointPath)
        || path.StartsWithSegments(AlivenessEndpointPath);
}
