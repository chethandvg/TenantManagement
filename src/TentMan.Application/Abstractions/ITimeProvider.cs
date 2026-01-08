namespace TentMan.Application.Abstractions;

public interface ITimeProvider
{
    DateTime UtcNow { get; }
}