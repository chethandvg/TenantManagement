using RichardSzalay.MockHttp;

namespace TentMan.AdminApiClient.Tests.TestHelpers;

/// <summary>
/// Factory for creating mock HTTP message handlers for testing.
/// </summary>
public static class MockHttpMessageHandlerFactory
{
    /// <summary>
    /// Creates a new mock HTTP message handler.
    /// </summary>
    /// <returns>A new <see cref="MockHttpMessageHandler"/> instance.</returns>
    public static MockHttpMessageHandler Create()
    {
        return new MockHttpMessageHandler();
    }

    /// <summary>
    /// Creates a configured HttpClient from a mock HTTP message handler.
    /// </summary>
    /// <param name="mockHttp">The mock HTTP message handler.</param>
    /// <param name="baseAddress">The base address for the HTTP client.</param>
    /// <returns>A configured <see cref="HttpClient"/> instance.</returns>
    public static HttpClient CreateHttpClient(
        MockHttpMessageHandler mockHttp,
        string baseAddress = "https://api.test.com")
    {
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri(baseAddress);
        return httpClient;
    }
}
