using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Peo.GestaoAlunos.Infra.Data.Contexts;
using System.Diagnostics;

namespace Peo.GestaoAlunos.Infra.Data.Helpers
{
    public static class GestaoAlunosDbMigrationHelpers
    {
        public static async Task UseGestaoAlunosDbMigrationHelperAsync(this WebApplication app)
        {
            await EnsureSeedDataAsync(app);
        }

        private static async Task EnsureSeedDataAsync(WebApplication serviceScope)
        {
            var services = serviceScope.Services.CreateScope().ServiceProvider;
            await EnsureSeedDataAsync(services);
        }

        private static async Task EnsureSeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>()
                                             .CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

            if (env.IsDevelopment() || env.EnvironmentName == "Docker")
            {
                var context = scope.ServiceProvider.GetRequiredService<GestaoAlunosContext>();

                try
                {
                    await context.Database.MigrateAsync();
                }
                catch (Exception ex)
                {
                    await context.Database.EnsureCreatedAsync();
                    Debug.WriteLine($"Migration warning: {ex.Message}");
                }
            }
        }
    }
}
