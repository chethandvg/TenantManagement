using RichardSzalay.MockHttp;
using System.Net;

namespace TentMan.AdminApiClient.Tests.TestHelpers;

/// <summary>
/// Factory for creating configured MockHttpMessageHandler instances for testing.
/// </summary>
public static class MockHttpMessageHandlerFactory
{
    /// <summary>
    /// Creates a basic MockHttpMessageHandler with a fallback for unmocked requests.
    /// </summary>
    /// <returns>Configured MockHttpMessageHandler instance.</returns>
    public static MockHttpMessageHandler CreateHandler()
    {
        var handler = new MockHttpMessageHandler();

        // Configure fallback for unmocked requests
        handler.Fallback.Respond(req =>
            new HttpResponseMessage(HttpStatusCode.NotImplemented)
            {
                RequestMessage = req,
                Content = new StringContent($"No mock configured for {req.Method} {req.RequestUri}")
            });

        return handler;
    }

    /// <summary>
    /// Creates a MockHttpMessageHandler that throws on unmocked requests.
    /// Use this in tests to ensure all HTTP calls are explicitly mocked.
    /// </summary>
    /// <returns>Configured MockHttpMessageHandler that throws on unmocked calls.</returns>
    public static MockHttpMessageHandler CreateStrictHandler()
    {
        var handler = new MockHttpMessageHandler();

        // Throw on unmocked requests to catch missing test setups
        handler.Fallback.Throw(new InvalidOperationException(
            "Unmocked HTTP request detected. All requests must be explicitly mocked in strict mode."));

        return handler;
    }

    /// <summary>
    /// Creates a basic HttpClient with MockHttpMessageHandler for testing.
    /// </summary>
    /// <param name="baseUrl">Base URL for the API client.</param>
    /// <returns>HttpClient configured with mock handler.</returns>
    public static HttpClient CreateClient(string baseUrl = "https://api.example.com/")
    {
        var handler = CreateHandler();
        var client = handler.ToHttpClient();
        client.BaseAddress = new Uri(baseUrl);
        return client;
    }
}
