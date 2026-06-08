using Chat.Application.Contracts;
using Chat.Domain.Entities;
using MediatR;

namespace Chat.Application.UseCases.GetConversations;

public sealed record GetConversationsQuery(Guid CurrentUserId) : IRequest<IReadOnlyCollection<Conversation>>;

public sealed class GetConversationsHandler(IChatRepository repository)
    : IRequestHandler<GetConversationsQuery, IReadOnlyCollection<Conversation>>
{
    public Task<IReadOnlyCollection<Conversation>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
        => repository.GetConversationsAsync(request.CurrentUserId, cancellationToken);
}
