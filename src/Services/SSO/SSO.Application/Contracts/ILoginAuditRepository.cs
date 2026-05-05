using SSO.Domain.Entities;

namespace SSO.Application.Contracts;

public interface ILoginAuditRepository
{
    Task AddAsync(LoginAudit item, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LoginAudit>> GetAllAsync(CancellationToken cancellationToken = default);
}
