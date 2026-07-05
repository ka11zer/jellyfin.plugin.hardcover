using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hardcover.Configuration;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hardcover.Api;

public class HardcoverApiService : IHardcoverApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HardcoverApiService> _logger;
    private readonly IConfigurationManager _configManager;

    // Simple cache: key -> (expiration, data)
    private static readonly ConcurrentDictionary<string, (DateTime expiry, object? data)> Cache = new();
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private static readonly SemaphoreSlim RateLimiter = new(1, 1);

    public HardcoverApiService(HttpClient httpClient, ILogger<HardcoverApiService> logger, IConfigurationManager configManager)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configManager = configManager;
    }

    private PluginConfiguration GetConfig() =>
        _configManager.GetPluginConfiguration<PluginConfiguration>(HardcoverPlugin.Instance!.Id);

    private async Task<T?> GetFromApiAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        var config = GetConfig();
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new InvalidOperationException("Hardcover API key is not configured.");

        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Add("Authorization", $"Bearer {config.ApiKey}");

        // Respect rate limits (simple 1 request per second)
        await RateLimiter.WaitAsync(cancellationToken);
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        }
        finally
        {
            // Release after a short delay to avoid hammering
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

    public async Task<IReadOnlyList<AuthorSearchResult>> SearchAuthorsAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var results = await GetCachedOrFetchAsync($"author_search_{name}", async () =>
            {
                return await GetFromApiAsync<List<AuthorSearchResult>>($"authors/search?q={Uri.EscapeDataString(name)}", cancellationToken);
            });
            return results ?? Array.Empty<AuthorSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching authors for {Name}", name);
            return Array.Empty<AuthorSearchResult>();
        }
    }

    public async Task<AuthorDetails?> GetAuthorByIdAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            return await GetCachedOrFetchAsync($"author_{slug}", async () =>
            {
                return await GetFromApiAsync<AuthorDetails>($"authors/{slug}", cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching author by slug {Slug}", slug);
            return null;
        }
    }

    public async Task<IReadOnlyList<BookSearchResult>> SearchBooksAsync(string title, CancellationToken cancellationToken)
    {
        try
        {
            var results = await GetCachedOrFetchAsync($"book_search_{title}", async () =>
            {
                return await GetFromApiAsync<List<BookSearchResult>>($"books/search?q={Uri.EscapeDataString(title)}", cancellationToken);
            });
            return results ?? Array.Empty<BookSearchResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching books for {Title}", title);
            return Array.Empty<BookSearchResult>();
        }
    }

    public async Task<BookDetails?> GetBookByIdAsync(string slug, CancellationToken cancellationToken)
    {
        try
        {
            return await GetCachedOrFetchAsync($"book_{slug}", async () =>
            {
                return await GetFromApiAsync<BookDetails>($"books/{slug}", cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching book by slug {Slug}", slug);
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> GetBookCoverUrlsAsync(string slug, CancellationToken cancellationToken)
    {
        // Hardcover returns a single cover URL in the book details; we wrap it as a list.
        var details = await GetBookByIdAsync(slug, cancellationToken);
        if (details?.CoverImageUrl != null)
            return new[] { details.CoverImageUrl };
        return Array.Empty<string>();
    }
}
