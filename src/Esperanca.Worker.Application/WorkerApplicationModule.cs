using Esperanca.Worker.Application._Shared.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Esperanca.Worker.Application;

public static class WorkerApplicationModule
{
    public static IServiceCollection ConfigureServices(IServiceCollection services)
    {
        var assembly = typeof(WorkerApplicationModule).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
