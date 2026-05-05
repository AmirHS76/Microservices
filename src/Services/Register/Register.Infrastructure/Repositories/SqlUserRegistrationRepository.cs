using Register.Application.Contracts;
using Register.Domain.Entities;
using Register.Infrastructure.Persistence;

namespace Register.Infrastructure.Repositories;

public sealed class SqlUserRegistrationRepository(RegisterDbContext dbContext) : IUserRegistrationRepository
{
    public async Task AddAsync(RegisteredUser user, CancellationToken cancellationToken = default)
    {
        await dbContext.RegisteredUsers.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
