using MediatR;
using User.Application.Contracts;
using User.Domain.Entities;

namespace User.Application.UseCases.GetUsers;

public sealed record GetUsersQuery : IRequest<IReadOnlyCollection<UserProfile>>;

public sealed class GetUsersHandler(IUserProfileRepository repository)
    : IRequestHandler<GetUsersQuery, IReadOnlyCollection<UserProfile>>
{
    public Task<IReadOnlyCollection<UserProfile>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        => repository.GetAllAsync(cancellationToken);
}
