using Peo.ServiceDefaults;
using Peo.Web.Bff.Services.Faturamento;
using Peo.Web.Bff.Services.Faturamento.Dtos;
using Peo.Web.Bff.Services.Handlers;

namespace Peo.Web.Bff.Configuration
{
    public static class FaturamentoDependencies
    {
        public static IServiceCollection AddFaturamento(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<FaturamentoService>();
            services.AddTransient<HttpClientAuthorizationDelegatingHandler>();

            services.AddHttpClient<FaturamentoService>(c =>
                c.BaseAddress = new Uri(configuration.GetValue<string>("Endpoints:Faturamento") ?? "https://peo-faturamento-webapi"))
                .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>()
                .AddCustomResilienceHandler(); // Retry + Circuit Breaker + Timeout

            return services;
        }

        public static WebApplication AddFaturamentoEndpoints(this WebApplication app)
        {
            var endpoints = app
            .MapGroup("v1/faturamento")
            .WithTags("Faturamento");

            endpoints.MapPost("/pagamento", async (EfetuarPagamentoRequest request, FaturamentoService service, CancellationToken ct) =>
            {
                return await service.EfetuarPagamentoAsync(request, ct);
            });

            return app;
        }
    }
}