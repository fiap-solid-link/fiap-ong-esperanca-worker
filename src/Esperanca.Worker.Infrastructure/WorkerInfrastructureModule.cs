using Esperanca.Worker.Application._Shared;
using Esperanca.Worker.Infrastructure._Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Esperanca.Worker.Infrastructure;

public static class WorkerInfrastructureModule
{
    public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // PostgreSQL + EF Core
        services.AddDbContext<WorkerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("WorkerDb")));

        // Repositories
        // services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<WorkerDbContext>());
        
        return services;
    }
}
