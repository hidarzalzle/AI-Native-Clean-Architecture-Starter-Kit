using Application.Abstractions;
using Application.Commands;
using Domain.Entities;
using Moq;
using Xunit;

namespace UnitTests;

public class CreateTicketHandlerTests
{
    [Fact]
    public async Task Creates_ticket_and_outbox()
    {
        var db = new Mock<IApplicationDbContext>();
        var outbox = new Mock<IOutboxWriter>();
        var clock = new Mock<IClock>();
        clock.SetupGet(x => x.UtcNow).Returns(DateTime.UtcNow);
        var handler = new CreateTicketHandler(db.Object, clock.Object, outbox.Object);

        _ = await handler.Handle(new CreateTicketCommand("x", "y", "a@b.com", "k1"), default);

        db.Verify(x => x.AddTicketAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Once);
        outbox.Verify(x => x.WriteDomainEventsAsync(It.IsAny<IEnumerable<object>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
