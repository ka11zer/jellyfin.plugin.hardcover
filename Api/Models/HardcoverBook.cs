using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hardcover.Api.Models;

/// <summary>
/// A book record as returned by the Hardcover "books" GraphQL table.
/// Field names mirror https://docs.hardcover.app/api/graphql/schemas/books/ exactly,
/// since Hasura tables use snake_case.
/// </summary>
public class HardcoverBook
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("pages")]
    public int? Pages { get; set; }

    /// <summary>
    /// Gets or sets the release date, formatted as "yyyy-MM-dd" (may be partial/null for unreleased books).
    /// </summary>
    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("ratings_count")]
    public int? RatingsCount { get; set; }

    /// <summary>
    /// Gets or sets the number of Hardcover users who have this book in their library.
    /// Used to disambiguate identically-titled books by popularity.
    /// </summary>
    [JsonPropertyName("users_count")]
    public int? UsersCount { get; set; }

    [JsonPropertyName("image")]
    public HardcoverImage? Image { get; set; }

    [JsonPropertyName("contributions")]
    public List<HardcoverContribution>? Contributions { get; set; }

    /// <summary>
    /// Gets or sets free-form community tags (genres, moods, etc.), returned as raw JSON by Hardcover.
    /// Kept as a raw <see cref="JsonElement"/> since the shape (object of arrays) isn't part of the
    /// stable schema; callers can walk it defensively without risking a deserialization failure.
    /// </summary>
    [JsonPropertyName("cached_tags")]
    public JsonElement? CachedTags { get; set; }
}

/// <summary>
/// A single contributor entry linking a book to an author (and their role, e.g. author vs translator).
/// </summary>
public class HardcoverContribution
{
    [JsonPropertyName("author")]
    public HardcoverAuthor? Author { get; set; }

    /// <summary>
    /// Gets or sets the contribution role/type (e.g. "Author", "Translator", "Illustrator"). Null usually means primary author.
    /// </summary>
    [JsonPropertyName("contribution")]
    public string? Contribution { get; set; }
}

/// <summary>
/// An author record as returned by the Hardcover "authors" GraphQL table.
/// </summary>
public class HardcoverAuthor
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("image")]
    public HardcoverImage? Image { get; set; }

    [JsonPropertyName("born_date")]
    public string? BornDate { get; set; }

    [JsonPropertyName("death_date")]
    public string? DeathDate { get; set; }

    [JsonPropertyName("books_count")]
    public int? BooksCount { get; set; }
}

/// <summary>
/// A Hardcover-hosted image reference.
/// </summary>
public class HardcoverImage
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
