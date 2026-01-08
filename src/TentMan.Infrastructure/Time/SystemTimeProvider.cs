using TentMan.Application.Abstractions;

namespace TentMan.Infrastructure.Time;

public sealed class SystemTimeProvider : ITimeProvider
{
    private readonly TimeProvider _provider;
    public SystemTimeProvider(TimeProvider provider) => _provider = provider;

    public DateTime UtcNow => _provider.GetUtcNow().UtcDateTime;
}