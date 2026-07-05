using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Jellyfin.Plugin.Hardcover.Api;

public interface IHardcoverApiService
{
    Task<IReadOnlyList<AuthorSearchResult>> SearchAuthorsAsync(string name, CancellationToken cancellationToken);
    Task<AuthorDetails?> GetAuthorByIdAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookSearchResult>> SearchBooksAsync(string title, CancellationToken cancellationToken);
    Task<BookDetails?> GetBookByIdAsync(string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetBookCoverUrlsAsync(string slug, CancellationToken cancellationToken);
}

// DTOs
public class AuthorSearchResult
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class AuthorDetails
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Biography { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? BookSlugs { get; set; }
}

public class BookSearchResult
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
}

public class BookDetails
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Authors { get; set; } = new();
    public string? CoverImageUrl { get; set; }
    public int? PublicationYear { get; set; }
    public string? Publisher { get; set; }
}
