using Register.Domain.Entities;

namespace Register.Application.Contracts;

public interface IUserRegistrationRepository
{
    Task AddAsync(RegisteredUser user, CancellationToken cancellationToken = default);
    Task<RegisteredUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<RegisteredUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
