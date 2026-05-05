using Register.Domain.Entities;

namespace Register.Application.Contracts;

public interface IUserRegistrationRepository
{
    Task AddAsync(RegisteredUser user, CancellationToken cancellationToken = default);
}
