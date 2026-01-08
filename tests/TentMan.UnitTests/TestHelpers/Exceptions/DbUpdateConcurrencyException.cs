namespace TentMan.UnitTests.TestHelpers.Exceptions;

/// <summary>
/// Test-only exception that mimics EF Core's DbUpdateConcurrencyException.
/// Used to test concurrency handling without requiring a full EF Core dependency in unit tests.
/// </summary>
/// <remarks>
/// This allows us to verify the concurrency catch blocks in handlers that check for exceptions
/// where ex.GetType().Name == "DbUpdateConcurrencyException"
/// </remarks>
public class DbUpdateConcurrencyException : Exception
{
    public DbUpdateConcurrencyException()
        : base("A database concurrency violation occurred.")
    {
    }

    public DbUpdateConcurrencyException(string message)
        : base(message)
    {
    }

    public DbUpdateConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
