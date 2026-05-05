using User.Domain.Entities;

namespace User.Application.Contracts;

public interface IUserProfileRepository
{
    Task UpsertAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
