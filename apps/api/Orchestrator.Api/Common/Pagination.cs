using System;
using System.Collections.Generic;

namespace Orchestrator.Api.Common
{
    public class PaginatedRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public static class PaginationExtensions
    {
        public static string GetOrderByClause(this PaginatedRequest request, string defaultSortBy = "Id")
        {
            var sortBy = string.IsNullOrEmpty(request.SortBy) ? defaultSortBy : request.SortBy;
            return $"{sortBy} {(request.SortDescending ? "DESC" : "ASC")}";
        }

        public static string GetPaginationClause(this PaginatedRequest request)
        {
            var offset = (request.PageNumber - 1) * request.PageSize;
            return $"LIMIT {request.PageSize} OFFSET {offset}";
        }
    }
} 