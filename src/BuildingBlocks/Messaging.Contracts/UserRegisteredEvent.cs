using Messaging.Abstractions;

namespace Messaging.Contracts;

public sealed record UserRegisteredEvent(Guid UserId, string Username, string Email, string Password) : IIntegrationEvent;
