using Domain.Enums;

namespace Domain.Events;

public record TicketCreated(Guid TicketId, DateTime OccurredAtUtc) : IDomainEvent;
public record TicketClassified(Guid TicketId, TicketCategory Category, TicketPriority Priority, DateTime OccurredAtUtc) : IDomainEvent;
public record TicketAssigned(Guid TicketId, string Assignee, DateTime OccurredAtUtc) : IDomainEvent;
