using Application.Abstractions;

namespace Infrastructure.Security;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
