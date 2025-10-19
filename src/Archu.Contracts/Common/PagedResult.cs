namespace Archu.Contracts.Common;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public record PagedResult<T>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IEnumerable<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Gets the page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total count of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// Creates a new paginated result.
    /// </summary>
    public static PagedResult<T> Create(IEnumerable<T> items, int pageNumber, int pageSize, int totalCount) => new()
    {
        Items = items,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = totalCount
    };
}

/// <summary>
/// Represents pagination parameters for API requests.
/// </summary>
public record PaginationParameters
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    /// <summary>
    /// Gets or sets the page number (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }
}
