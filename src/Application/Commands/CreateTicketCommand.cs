using Application.Abstractions;
using Application.Dtos;
using Domain.Entities;
using MediatR;
using SharedKernel.Common;

namespace Application.Commands;

public record CreateTicketCommand(string Title, string Description, string CustomerEmail, string IdempotencyKey) : IRequest<TicketDto>, IIdempotentCommand;

public class CreateTicketHandler(IApplicationDbContext db, IClock clock, IOutboxWriter outboxWriter) : IRequestHandler<CreateTicketCommand, TicketDto>
{
    public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        using var activity = AppDiagnostics.ActivitySource.StartActivity("ticket.create");
        var ticket = Ticket.Create(request.Title, request.Description, request.CustomerEmail, clock.UtcNow);
        activity?.SetTag("ticket.id", ticket.Id.ToString());
        await db.AddTicketAsync(ticket, ct);
        await outboxWriter.WriteDomainEventsAsync(ticket.DomainEvents, ct);
        await db.SaveChangesAsync(ct);
        ticket.ClearDomainEvents();
        return new TicketDto(ticket.Id, ticket.Title, ticket.Description, ticket.CustomerEmail, ticket.Status, ticket.Priority, ticket.Category, ticket.CreatedAtUtc, ticket.UpdatedAtUtc);
    }
}
