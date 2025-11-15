using Peo.ServiceDefaults;
using Peo.Web.Bff.Services.Handlers;
using Peo.Web.Bff.Services.Identity;
using Peo.Web.Bff.Services.Identity.Dtos;

namespace Peo.Web.Bff.Configuration
{
    public static class IdentityDependencies
    {
        public static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IdentityService>();
            services.AddTransient<HttpClientAuthorizationDelegatingHandler>();

            services.AddHttpClient<IdentityService>(c =>
                c.BaseAddress = new Uri(configuration.GetValue<string>("Endpoints:Identity") ?? "https://peo-identity-webapi"))
                .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>()
                .AddCustomResilienceHandler(); // Retry + Circuit Breaker + Timeout

            return services;
        }

        public static WebApplication AddIdentityEndpoints(this WebApplication app)
        {
            var endpoints = app
            .MapGroup("v1/identity")
            .WithTags("Identity");

            endpoints.MapPost("/register", async (RegisterRequest request, IdentityService service, CancellationToken ct) =>
            {
                return await service.RegisterAsync(request, ct);
            });

            endpoints.MapPost("/login", async (LoginRequest request, IdentityService service, CancellationToken ct) =>
            {
                return await service.LoginAsync(request, ct);
            });

            endpoints.MapPost("/refresh-token", async (RefreshTokenRequest request, IdentityService service, CancellationToken ct) =>
            {
                return await service.RefreshTokenAsync(request, ct);
            });

            return app;
        }
    }
}