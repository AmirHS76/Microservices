using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Chat.Api.Realtime;

public sealed class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? connection.User?.FindFirstValue("sub");
}
