using GraphQL.Client.Http;

namespace Archu.Infrastructure.Contentful;

/// <summary>
/// Defines the contract for creating and managing GraphQL HTTP clients.
/// This abstraction allows for different implementations based on environment (preview vs production).
/// </summary>
public interface IGraphQlClientService
{
    /// <summary>
    /// Gets a configured GraphQL HTTP client.
    /// </summary>
    /// <param name="isPreview">If true, returns a client configured for preview/draft content; otherwise, returns a client for published content.</param>
    /// <returns>A configured <see cref="GraphQLHttpClient"/> instance.</returns>
    GraphQLHttpClient GetGraphQLClient(bool isPreview = false);
}
