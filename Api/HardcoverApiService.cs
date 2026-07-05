using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hardcover.Api;

public interface IHardcoverApiService
{
    Task<List<AuthorSearchResult>> SearchAuthorsAsync(string name, CancellationToken cancellationToken);
    Task<AuthorDetails?> GetAuthorByIdAsync(string slug, CancellationToken cancellationToken);
    Task<List<BookSearchResult>> SearchBooksAsync(string title, CancellationToken cancellationToken);
    Task<BookDetails?> GetBookByIdAsync(string slug, CancellationToken cancellationToken);
    Task<List<string>> GetBookCoverUrlsAsync(string slug, CancellationToken cancellationToken);
}

public class HardcoverApiService : IHardcoverApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HardcoverApiService> _logger;

    private static readonly ConcurrentDictionary<string, (DateTime expiry, object? data)> Cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private static readonly SemaphoreSlim RateLimiter = new(1, 1);

    public HardcoverApiService(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _logger = loggerFactory.CreateLogger<HardcoverApiService>();
    }

    private string GetApiKey()
    {
        var config = HardcoverPlugin.Instance?.Configuration;
        return config?.ApiKey ?? throw new InvalidOperationException("Hardcover API key is not configured.");
    }

    private async Task<T?> GetFromApiAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        var apiKey = GetApiKey();
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        await RateLimiter.WaitAsync(cancellationToken);
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        finally
        {
            _ = Task.Delay(1000).ContinueWith(_ => RateLimiter.Release());
        }
    }

    private async Task<T?> GetCachedOrFetchAsync<T>(string cacheKey, Func<Task<T?>> fetchFunc)
    {
        if (Cache.TryGetValue(cacheKey, out var entry) && entry.expiry > DateTime.UtcNow)
            return (T?)entry.data;

        var result = await fetchFunc();
        Cache[cacheKey] = (DateTime.UtcNow + CacheDuration, result);
        return result;
    }

    public async Task<List<AuthorSearchResult>> SearchAuthorsAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            return await GetCachedOrFetchAsync($"author_search_{name}", async () =>
                await GetFromApiAsync<List<AuthorSearchResult>>($"authors/search?q={Uri.EscapeDataString(name)}", cancellationToken))
                ?? new List<AuthorSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching authors for {Name}", name);
            return new List<AuthorSearchResult>();
        }
    }

    public async Task<AuthorDetails?> GetAuthorByIdAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            return await GetCachedOrFetchAsync($"author_{slug}", async () =>
                await GetFromApiAsync<AuthorDetails>($"authors/{slug}", cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching author by slug {Slug}", slug);
            return null;
        }
    }

    public async Task<List<BookSearchResult>> SearchBooksAsync(string title, CancellationToken cancellationToken)
    {
        try
        {
            return await GetCachedOrFetchAsync($"book_search_{title}", async () =>
                await GetFromApiAsync<List<BookSearchResult>>($"books/search?q={Uri.EscapeDataString(title)}", cancellationToken))
                ?? new List<BookSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books for {Title}", title);
            return new List<BookSearchResult>();
        }
    }

    public async Task<BookDetails?> GetBookByIdAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            return await GetCachedOrFetchAsync($"book_{slug}", async () =>
                await GetFromApiAsync<BookDetails>($"books/{slug}", cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching book by slug {Slug}", slug);
            return null;
        }
    }

    public async Task<List<string>> GetBookCoverUrlsAsync(string slug, CancellationToken cancellationToken)
    {
        var details = await GetBookByIdAsync(slug, cancellationToken);
        if (details?.CoverImageUrl != null)
            return new List<string> { details.CoverImageUrl };
        return new List<string>();
    }
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
