using Microsoft.EntityFrameworkCore;
using User.Application.Contracts;
using User.Domain.Entities;
using User.Infrastructure.Persistence;

namespace User.Infrastructure.Repositories;

public sealed class SqlUserProfileRepository(UserDbContext dbContext) : IUserProfileRepository
{
    public async Task UpsertAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.UserProfiles.FirstOrDefaultAsync(x => x.UserId == profile.UserId, cancellationToken);
        if (existing is null)
        {
            await dbContext.UserProfiles.AddAsync(profile, cancellationToken);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(profile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.UserProfiles
            .AsNoTracking()
            .OrderBy(x => x.Username)
            .ToListAsync(cancellationToken);
    }

    public Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
