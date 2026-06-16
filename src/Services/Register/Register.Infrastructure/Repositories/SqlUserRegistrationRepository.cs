using Microsoft.EntityFrameworkCore;
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

    public async Task<RegisteredUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await dbContext.RegisteredUsers.FirstOrDefaultAsync(x => x.Username == username, cancellationToken);
    }

    public async Task<RegisteredUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.RegisteredUsers.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }
}
