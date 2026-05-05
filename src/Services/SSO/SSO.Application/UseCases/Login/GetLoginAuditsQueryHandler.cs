using MediatR;
using SSO.Application.Contracts;
using SSO.Domain.Entities;

namespace SSO.Application.UseCases.Login;

public sealed class GetLoginAuditsQueryHandler(ILoginAuditRepository repository)
    : IRequestHandler<GetLoginAuditsQuery, IReadOnlyCollection<LoginAudit>>
{
    public Task<IReadOnlyCollection<LoginAudit>> Handle(GetLoginAuditsQuery query, CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);
}
