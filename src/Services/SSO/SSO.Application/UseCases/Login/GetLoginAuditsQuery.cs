using MediatR;
using SSO.Domain.Entities;

namespace SSO.Application.UseCases.Login;

public sealed record GetLoginAuditsQuery : IRequest<IReadOnlyCollection<LoginAudit>>;
