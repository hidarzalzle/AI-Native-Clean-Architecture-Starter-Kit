using Domain.Enums;

namespace Application.Dtos;

public record TicketDto(Guid Id, string Title, string Description, string CustomerEmail, TicketStatus Status, TicketPriority Priority, TicketCategory Category, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
