using OIO.Application.Abstractions.Clock;

namespace OIO.Infrastructure.Clock;

public sealed class DatetimeProvider : IClock
{
    private readonly TimeProvider _timeProvider;

    public DatetimeProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }
    
    public DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;
}