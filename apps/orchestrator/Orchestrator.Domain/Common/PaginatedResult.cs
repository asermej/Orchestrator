using System.Collections.Generic;

namespace Orchestrator.Domain;

/// <summary>
/// Represents a paginated result
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
public class PaginatedResult<T>
{
    /// <summary>
    /// The items in the current page
    /// </summary>
    public IEnumerable<T> Items { get; }

    /// <summary>
    /// The total number of items across all pages
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// The current page number (1-based)
    /// </summary>
    public int PageNumber { get; }

    /// <summary>
    /// The page size
    /// </summary>
    public int PageSize { get; }

    /// <summary>
    /// The total number of pages
    /// </summary>
    public int TotalPages { get; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage { get; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage { get; }

    /// <summary>
    /// Creates a new paginated result
    /// </summary>
    /// <param name="items">The items in the current page</param>
    /// <param name="totalCount">The total number of items across all pages</param>
    /// <param name="pageNumber">The current page number (1-based)</param>
    /// <param name="pageSize">The page size</param>
    public PaginatedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
    }

    /// <summary>
    /// Creates an empty paginated result
    /// </summary>
    /// <param name="pageNumber">The current page number (1-based)</param>
    /// <param name="pageSize">The page size</param>
    /// <returns>An empty paginated result</returns>
    public static PaginatedResult<T> Empty(int pageNumber = 1, int pageSize = 10)
    {
        return new PaginatedResult<T>(new List<T>(), 0, pageNumber, pageSize);
    }
}