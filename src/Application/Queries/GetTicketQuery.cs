using Application.Abstractions;
using Application.Dtos;
using MediatR;

namespace Application.Queries;

public record GetTicketQuery(Guid Id) : IRequest<TicketDto?>;

public class GetTicketHandler(IApplicationDbContext db) : IRequestHandler<GetTicketQuery, TicketDto?>
{
    public Task<TicketDto?> Handle(GetTicketQuery request, CancellationToken ct)
    {
        var t = db.Tickets.FirstOrDefault(x => x.Id == request.Id);
        return Task.FromResult(t is null ? null : new TicketDto(t.Id, t.Title, t.Description, t.CustomerEmail, t.Status, t.Priority, t.Category, t.CreatedAtUtc, t.UpdatedAtUtc));
    }
}
