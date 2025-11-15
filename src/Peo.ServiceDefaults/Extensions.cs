using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Peo.ServiceDefaults;

// Adds common .NET Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class Extensions
{
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

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

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
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
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

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static IHealthChecksBuilder AddDatabaseHealthChecks(this IHealthChecksBuilder healthChecks, IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            var sqliteConnection = configuration.GetConnectionString("SQLiteConnection");
            if (!string.IsNullOrEmpty(sqliteConnection))
            {
                healthChecks.AddSqlite(sqliteConnection, name: "database", tags: ["ready", "db"]);
            }
        }
        else
        {
            var sqlServerConnection = configuration.GetConnectionString("SqlServerConnection");
            if (!string.IsNullOrEmpty(sqlServerConnection))
            {
                healthChecks.AddSqlServer(sqlServerConnection, name: "database", tags: ["ready", "db"]);
            }
        }

        return healthChecks;
    }

    public static IHealthChecksBuilder AddRabbitMQHealthCheck(this IHealthChecksBuilder healthChecks, IConfiguration configuration)
    {
        // Try to get RabbitMQ connection string from messaging connection string (AMQP URI format)
        var messagingConnectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(messagingConnectionString))
        {
            healthChecks.AddRabbitMQ(
                sp =>
                {
                    var factory = new RabbitMQ.Client.ConnectionFactory();
                    factory.Uri = new Uri(messagingConnectionString);
                    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                },
                name: "rabbitmq",
                tags: ["ready", "messaging"]);
        }
        else
        {
            // Fallback: Try to build connection string from RabbitMQ section
            var rabbitMQSection = configuration.GetSection("RabbitMQ");
            var host = rabbitMQSection["Host"];
            var username = rabbitMQSection["Username"];
            var password = rabbitMQSection["Password"];
            var port = rabbitMQSection["Port"] ?? "5672";

            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var connectionString = $"amqp://{username}:{Uri.EscapeDataString(password)}@{host}:{port}";
                healthChecks.AddRabbitMQ(
                    sp =>
                    {
                        var factory = new RabbitMQ.Client.ConnectionFactory();
                        factory.Uri = new Uri(connectionString);
                        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                    },
                    name: "rabbitmq",
                    tags: ["ready", "messaging"]);
            }
        }

        return healthChecks;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // All health checks must pass for app to be considered ready to accept traffic after starting
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready")
        });

        // Only health checks tagged with the "live" tag must pass for app to be considered alive
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }

    /// <summary>
    /// Adiciona políticas de resiliência customizadas ao HttpClient.
    /// Inclui: Retry com exponential backoff, Circuit Breaker e Timeout.
    /// </summary>
    public static IHttpClientBuilder AddCustomResilienceHandler(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            // Retry Policy: 3 tentativas com exponential backoff
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true; // Adiciona jitter para evitar "thundering herd"

            // Circuit Breaker: Abre após 50% de falhas em 30 segundos
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5; // 50% de falhas
            options.CircuitBreaker.MinimumThroughput = 10; // Mínimo de 10 requisições
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30); // Aguarda 30s antes de tentar novamente

            // Timeout: 30 segundos por requisição
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        return builder;
    }

    /// <summary>
    /// Adiciona políticas de resiliência agressivas para serviços críticos.
    /// Retry mais rápido e Circuit Breaker mais sensível.
    /// </summary>
    public static IHttpClientBuilder AddAggressiveResilienceHandler(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler(options =>
        {
            // Retry: 5 tentativas com backoff rápido
            options.Retry.MaxRetryAttempts = 5;
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            // Circuit Breaker: Mais sensível - abre com 30% de falhas
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(15);
            options.CircuitBreaker.FailureRatio = 0.3; // 30% de falhas
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);

            // Timeout: 10 segundos (mais agressivo)
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(10);
        });

        return builder;
    }
}
