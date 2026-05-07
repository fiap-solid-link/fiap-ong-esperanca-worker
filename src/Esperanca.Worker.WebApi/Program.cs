using Esperanca.Worker.WebApi;
using Esperanca.Worker.WebApi._Shared.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

WorkerWebApiModule.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ValidationExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Esperanca Worker API v1"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

namespace Esperanca.Worker.WebApi
{
    public partial class Program;
}