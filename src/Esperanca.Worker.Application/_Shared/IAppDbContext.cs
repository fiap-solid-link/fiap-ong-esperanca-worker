using Microsoft.EntityFrameworkCore;

namespace Esperanca.Worker.Application._Shared;

public interface IAppDbContext
{
    DbSet<T> Set<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
