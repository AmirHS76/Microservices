using SSO.Application.Contracts;
using SSO.Domain.Entities;
using SSO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SSO.Infrastructure.Repositories;

public sealed class SqlLoginAuditRepository(SsoDbContext dbContext) : ILoginAuditRepository
{
    public async Task AddAsync(LoginAudit item, CancellationToken cancellationToken = default)
    {
        await dbContext.LoginAudits.AddAsync(item, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LoginAudit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.LoginAudits
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);
    }
}
