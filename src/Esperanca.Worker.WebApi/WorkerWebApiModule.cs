using Esperanca.Worker.Application;
using Esperanca.Worker.Infrastructure;
using Microsoft.OpenApi.Models;

namespace Esperanca.Worker.WebApi;

public static class WorkerWebApiModule
{
    public static IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Modules
        WorkerApplicationModule.ConfigureServices(services);
        WorkerInfrastructureModule.ConfigureServices(services, configuration);

        services.AddHttpContextAccessor();

        // Controllers
        services.AddControllers();

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "Esperanca Worker API",
                Version     = "v1",
                Description = "API de Worker - Plataforma Conexao Solidaria"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name         = "Authorization",
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = ParameterLocation.Header,
                Description  = "Insira o token JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            options.TagActionsBy(api => api.GroupName is not null
                ? [api.GroupName]
                : api.ActionDescriptor.EndpointMetadata
                    .OfType<TagsAttribute>()
                    .SelectMany(t => t.Tags)
                    .DefaultIfEmpty("Outros")
                    .ToList());

            options.OrderActionsBy(apiDesc => apiDesc.GroupName);
        });

        // Health Checks
        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("WorkerDb")!,
                name: "postgresql",
                tags: ["db", "ready"]);

        return services;
    }
}
