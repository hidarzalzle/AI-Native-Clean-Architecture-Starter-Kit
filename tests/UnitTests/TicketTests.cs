using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace UnitTests;

public class TicketTests
{
    [Fact]
    public void Cannot_classify_non_new_ticket()
    {
        var t = Ticket.Create("Title", "Desc", "a@b.com", DateTime.UtcNow);
        t.Classify(TicketCategory.Bug, TicketPriority.High, DateTime.UtcNow);
        Assert.Throws<InvalidOperationException>(() => t.Classify(TicketCategory.Billing, TicketPriority.Low, DateTime.UtcNow));
    }
}
