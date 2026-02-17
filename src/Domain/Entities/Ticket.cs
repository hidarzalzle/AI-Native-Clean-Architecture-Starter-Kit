using Domain.Enums;
using Domain.Events;
using SharedKernel.Common;

namespace Domain.Entities;

public class Ticket : Entity
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = default!;
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketCategory Category { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private Ticket() { }

    public static Ticket Create(string title, string description, string customerEmail, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 200) throw new ArgumentException("Title is required and <= 200 chars.");
        if (!IsValidEmail(customerEmail)) throw new ArgumentException("CustomerEmail invalid.");

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CustomerEmail = customerEmail,
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Other,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        ticket.AddDomainEvent(new TicketCreated(ticket.Id, nowUtc));
        return ticket;
    }

    public void Classify(TicketCategory category, TicketPriority priority, DateTime nowUtc)
    {
        if (Status != TicketStatus.New) throw new InvalidOperationException("Only new ticket can be classified.");
        Category = category;
        Priority = priority;
        Status = TicketStatus.Classified;
        UpdatedAtUtc = nowUtc;
        AddDomainEvent(new TicketClassified(Id, category, priority, nowUtc));
    }

    private static bool IsValidEmail(string email) => !string.IsNullOrWhiteSpace(email) && email.Contains('@') && email.Contains('.');
}
