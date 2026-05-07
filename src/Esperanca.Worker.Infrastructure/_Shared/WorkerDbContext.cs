using Esperanca.Worker.Application._Shared;
using Microsoft.EntityFrameworkCore;

namespace Esperanca.Worker.Infrastructure._Shared;

public class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options), IAppDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkerDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
