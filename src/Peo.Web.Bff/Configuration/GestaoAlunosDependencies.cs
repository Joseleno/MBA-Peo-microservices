using Peo.Web.Bff.Services.GestaoAlunos;
using Peo.Web.Bff.Services.GestaoAlunos.Dtos;
using Peo.Web.Bff.Services.Handlers;
using Peo.Web.Bff.Services.Helpers;
using Polly;

namespace Peo.Web.Bff.Configuration
{
    public static class GestaoAlunosDependencies
    {
        public static IServiceCollection AddGestaoAlunos(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<GestaoAlunosService>();
            services.AddTransient<HttpClientAuthorizationDelegatingHandler>();

            services.AddHttpClient<GestaoAlunosService>(c =>
                c.BaseAddress = new Uri(configuration.GetValue<string>("Endpoints:GestaoAlunos")!))
                .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>()
                .AddPolicyHandler(PollyExtensions.WaitAndRetry())
                .AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

            return services;
        }

        public static WebApplication AddGestaoAlunosEndpoints(this WebApplication app)
        {
            var endpoints = app
            .MapGroup("v1/aluno")
            .WithTags("Alunos");

            endpoints.MapPost("/matricula/", async (MatriculaCursoRequest request, GestaoAlunosService service, CancellationToken ct) =>
            {
                return await service.MatricularCursoAsync(request, ct);
            });

            endpoints.MapGet("/matricula/", async (GestaoAlunosService service, CancellationToken ct) =>
            {
                return await service.ConsultarMatriculasAlunoAsync(ct);
            });

            endpoints.MapPost("/matricula/concluir", async (ConcluirMatriculaRequest request, GestaoAlunosService service, CancellationToken ct) =>
            {
                return await service.ConcluirMatriculaAsync(request, ct);
            });

            // Aula endpoints
            endpoints.MapPost("/matricula/aula/iniciar", async (IniciarAulaRequest request, GestaoAlunosService service, CancellationToken ct) =>
            {
                return await service.IniciarAulaAsync(request, ct);
            });

            endpoints.MapPost("/matricula/aula/concluir", async (ConcluirAulaRequest request, GestaoAlunosService service, CancellationToken ct) =>
            {
                return await service.ConcluirAulaAsync(request, ct);
            });

            // Certificados endpoint
            endpoints.MapGet("/certificados", async (GestaoAlunosService service, CancellationToken ct) =>
            {
                return await service.ObterCertificadosAsync(ct);
            });

            // Hist�rico endpoint
            endpoints.MapGet("/historico", async (GestaoAlunosService service, CancellationToken ct) =>
            {
                return await service.ObterHistoricoAsync(ct);
            });

            return app;
        }
    }
}