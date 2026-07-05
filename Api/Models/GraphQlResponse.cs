using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hardcover.Api.Models;

/// <summary>
/// Generic envelope for a GraphQL response as returned by the Hardcover API.
/// </summary>
/// <typeparam name="TData">The shape of the "data" payload for a given query.</typeparam>
public class GraphQlResponse<TData>
{
    /// <summary>
    /// Gets or sets the successful query payload, if any.
    /// </summary>
    [JsonPropertyName("data")]
    public TData? Data { get; set; }

    /// <summary>
    /// Gets or sets any GraphQL-level errors returned alongside (or instead of) data.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<GraphQlError>? Errors { get; set; }
}

/// <summary>
/// A single GraphQL error entry.
/// </summary>
public class GraphQlError
{
    /// <summary>
    /// Gets or sets the human-readable error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

/// <summary>
/// Wrapper for queries whose root field is "books".
/// </summary>
public class BooksData
{
    /// <summary>
    /// Gets or sets the list of matching books.
    /// </summary>
    [JsonPropertyName("books")]
    public List<HardcoverBook>? Books { get; set; }
}

/// <summary>
/// Wrapper for queries whose root field is "books_by_pk".
/// </summary>
public class BookByPkData
{
    /// <summary>
    /// Gets or sets the single book matched by primary key, or null if not found.
    /// </summary>
    [JsonPropertyName("books_by_pk")]
    public HardcoverBook? Book { get; set; }
}

/// <summary>
/// Wrapper for queries whose root field is "authors".
/// </summary>
public class AuthorsData
{
    /// <summary>
    /// Gets or sets the list of matching authors.
    /// </summary>
    [JsonPropertyName("authors")]
    public List<HardcoverAuthor>? Authors { get; set; }
}

/// <summary>
/// Wrapper for queries whose root field is "authors_by_pk".
/// </summary>
public class AuthorByPkData
{
    /// <summary>
    /// Gets or sets the single author matched by primary key, or null if not found.
    /// </summary>
    [JsonPropertyName("authors_by_pk")]
    public HardcoverAuthor? Author { get; set; }
}
