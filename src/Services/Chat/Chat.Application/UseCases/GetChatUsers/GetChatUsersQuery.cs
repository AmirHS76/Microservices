using Chat.Application.Contracts;
using Chat.Domain.Entities;
using MediatR;

namespace Chat.Application.UseCases.GetChatUsers;

public sealed record GetChatUsersQuery(Guid CurrentUserId) : IRequest<IReadOnlyCollection<ChatUser>>;

public sealed class GetChatUsersHandler(IReadChatRepository repository)
    : IRequestHandler<GetChatUsersQuery, IReadOnlyCollection<ChatUser>>
{
    public Task<IReadOnlyCollection<ChatUser>> Handle(GetChatUsersQuery request, CancellationToken cancellationToken)
        => repository.GetUsersAsync(request.CurrentUserId, cancellationToken);
}
