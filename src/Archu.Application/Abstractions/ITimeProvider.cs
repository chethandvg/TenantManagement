namespace Archu.Application.Abstractions;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}