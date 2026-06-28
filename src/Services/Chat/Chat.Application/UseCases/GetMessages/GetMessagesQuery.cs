using Chat.Application.Contracts;
using Chat.Domain.Entities;
using MediatR;

namespace Chat.Application.UseCases.GetMessages;

public sealed record GetMessagesQuery(Guid CurrentUserId, Guid OtherUserId, int PageNumber, int PageSize) : IRequest<IReadOnlyCollection<ChatMessage>>;

public sealed class GetMessagesHandler(IReadChatRepository repository)
    : IRequestHandler<GetMessagesQuery, IReadOnlyCollection<ChatMessage>>
{
    public Task<IReadOnlyCollection<ChatMessage>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
        => repository.GetMessagesAsync(request.CurrentUserId, request.OtherUserId, request.PageNumber, request.PageSize, cancellationToken);
}
