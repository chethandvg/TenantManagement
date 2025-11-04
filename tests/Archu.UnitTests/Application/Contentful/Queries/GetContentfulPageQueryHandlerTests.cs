using Archu.Application.Abstractions;
using Archu.Application.Contentful.Models;
using Archu.Application.Contentful.Queries.GetContentfulPage;
using Archu.Contracts.Contentful;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Archu.UnitTests.Application.Contentful.Queries;

[Trait("Category", "Unit")]
[Trait("Feature", "Contentful")]
public class GetContentfulPageQueryHandlerTests
{
    private readonly Mock<IContentfulService> _contentfulServiceMock;
    private readonly Mock<ILogger<GetContentfulPageQueryHandler>> _loggerMock;
    private readonly GetContentfulPageQueryHandler _handler;

    public GetContentfulPageQueryHandlerTests()
    {
        _contentfulServiceMock = new Mock<IContentfulService>();
        _loggerMock = new Mock<ILogger<GetContentfulPageQueryHandler>>();
        _handler = new GetContentfulPageQueryHandler(_contentfulServiceMock.Object, _loggerMock.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WhenPageExists_ReturnsPageDto()
    {
        // Arrange
        var pageUrl = "home";
        var locale = "en-US";
        var contentfulPage = new ContentfulPage
        {
            Slug = pageUrl,
            Title = "Home Page",
            Description = "Welcome to our home page",
            Sections = new List<ContentfulSection>
            {
                new ContentfulSection
                {
                    Id = "section1",
                    ContentType = "heroSection",
                    Fields = new Dictionary<string, object?>
                    {
                        { "heading", "Welcome" },
                        { "text", "This is a hero section" }
                    }
                }
            }
        };

        _contentfulServiceMock
            .Setup(x => x.GetPageAsync(pageUrl, locale, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentfulPage);

        var query = new GetContentfulPageQuery(pageUrl, locale);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be(pageUrl);
        result.Title.Should().Be("Home Page");
        result.Description.Should().Be("Welcome to our home page");
        result.Sections.Should().HaveCount(1);
        result.Sections[0].Id.Should().Be("section1");
        result.Sections[0].ContentType.Should().Be("heroSection");
        result.Sections[0].Fields.Should().ContainKey("heading");
    }

    [Fact]
    public async Task Handle_WhenPageExistsWithoutLocale_UsesDefaultLocale()
    {
        // Arrange
        var pageUrl = "about";
        var contentfulPage = new ContentfulPage
        {
            Slug = pageUrl,
            Title = "About Us",
            Description = "Learn more about us",
            Sections = new List<ContentfulSection>()
        };

        _contentfulServiceMock
            .Setup(x => x.GetPageAsync(pageUrl, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentfulPage);

        var query = new GetContentfulPageQuery(pageUrl);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be(pageUrl);
        result.Title.Should().Be("About Us");
    }

    [Fact]
    public async Task Handle_WhenPageHasMultipleSections_MapsAllSections()
    {
        // Arrange
        var pageUrl = "features";
        var contentfulPage = new ContentfulPage
        {
            Slug = pageUrl,
            Title = "Features",
            Description = "Our amazing features",
            Sections = new List<ContentfulSection>
            {
                new ContentfulSection
                {
                    Id = "section1",
                    ContentType = "heroSection",
                    Fields = new Dictionary<string, object?> { { "heading", "Hero" } }
                },
                new ContentfulSection
                {
                    Id = "section2",
                    ContentType = "textBlock",
                    Fields = new Dictionary<string, object?> { { "text", "Some text" } }
                },
                new ContentfulSection
                {
                    Id = "section3",
                    ContentType = "imageGallery",
                    Fields = new Dictionary<string, object?> { { "images", new[] { "img1.jpg", "img2.jpg" } } }
                }
            }
        };

        _contentfulServiceMock
            .Setup(x => x.GetPageAsync(pageUrl, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentfulPage);

        var query = new GetContentfulPageQuery(pageUrl);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Sections.Should().HaveCount(3);
        result.Sections[0].ContentType.Should().Be("heroSection");
        result.Sections[1].ContentType.Should().Be("textBlock");
        result.Sections[2].ContentType.Should().Be("imageGallery");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Handle_WhenPageDoesNotExist_ReturnsNull()
    {
        // Arrange
        var pageUrl = "nonexistent";
        _contentfulServiceMock
            .Setup(x => x.GetPageAsync(pageUrl, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentfulPage?)null);

        var query = new GetContentfulPageQuery(pageUrl);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenServiceThrowsException_PropagatesException()
    {
        // Arrange
        var pageUrl = "error-page";
        _contentfulServiceMock
            .Setup(x => x.GetPageAsync(pageUrl, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Contentful service error"));

        var query = new GetContentfulPageQuery(pageUrl);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(query, CancellationToken.None));
    }

    #endregion

    #region Localization Tests

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("es-ES")]
    public async Task Handle_WithDifferentLocales_PassesLocaleToService(string locale)
    {
        // Arrange
        var pageUrl = "home";
        var contentfulPage = new ContentfulPage
        {
            Slug = pageUrl,
            Title = "Home Page",
            Sections = new List<ContentfulSection>()
        };

        _contentfulServiceMock
            .Setup(x => x.GetPageAsync(pageUrl, locale, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentfulPage);

        var query = new GetContentfulPageQuery(pageUrl, locale);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _contentfulServiceMock.Verify(
            x => x.GetPageAsync(pageUrl, locale, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Mapping Tests

    [Fact]
    public async Task Handle_MapsAllPageProperties()
    {
        // Arrange
        var pageUrl = "test-page";
        var contentfulPage = new ContentfulPage
        {
            Slug = "test-page",
            Title = "Test Page Title",
            Description = "Test Page Description",
            Sections = new List<ContentfulSection>()
        };

        _contentfulServiceMock
            .Setup(x => x.GetPageAsync(pageUrl, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentfulPage);

        var query = new GetContentfulPageQuery(pageUrl);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Slug.Should().Be(contentfulPage.Slug);
        result.Title.Should().Be(contentfulPage.Title);
        result.Description.Should().Be(contentfulPage.Description);
        result.Sections.Should().BeEquivalentTo(
            contentfulPage.Sections,
            options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task Handle_WhenSectionFieldsAreNull_HandlesGracefully()
    {
        // Arrange
        var pageUrl = "minimal-page";
        var contentfulPage = new ContentfulPage
        {
            Slug = pageUrl,
            Title = "Minimal Page",
            Description = null,
            Sections = new List<ContentfulSection>
            {
                new ContentfulSection
                {
                    Id = "section1",
                    ContentType = "emptySection",
                    Fields = new Dictionary<string, object?>()
                }
            }
        };

        _contentfulServiceMock
            .Setup(x => x.GetPageAsync(pageUrl, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentfulPage);

        var query = new GetContentfulPageQuery(pageUrl);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().BeNull();
        result.Sections.Should().HaveCount(1);
        result.Sections[0].Fields.Should().BeEmpty();
    }

    #endregion
}
