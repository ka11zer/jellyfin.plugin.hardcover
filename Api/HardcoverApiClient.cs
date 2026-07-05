using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hardcover.Api.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hardcover.Api;

/// <summary>
/// Thin wrapper around the Hardcover GraphQL API (https://docs.hardcover.app/api/).
/// Handles authentication, a conservative client-side rate limit (Hardcover allows
/// 60 requests/minute), and JSON (de)serialization.
/// </summary>
public class HardcoverApiClient
{
    /// <summary>
    /// The named <see cref="HttpClient"/> registered in <see cref="PluginServiceRegistrator"/>.
    /// </summary>
    public const string HttpClientName = "Hardcover";

    /// <summary>
    /// The Hardcover GraphQL endpoint. All queries and mutations go through this single URL.
    /// </summary>
    public const string GraphQlEndpoint = "https://api.hardcover.app/v1/graphql";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    // Hardcover enforces a hard limit of 60 requests/minute per token. We stay comfortably
    // under that with a simple "at most one request every 1.1s" gate rather than a full
    // sliding-window limiter -- metadata lookups are not high throughput.
    private static readonly SemaphoreSlim RateGate = new(1, 1);
    private static DateTime _lastCallUtc = DateTime.MinValue;
    private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(1100);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HardcoverApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardcoverApiClient"/> class.
    /// </summary>
    public HardcoverApiClient(IHttpClientFactory httpClientFactory, ILogger<HardcoverApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether an API token has been configured by the user.
    /// </summary>
    public static bool HasApiToken =>
        !string.IsNullOrWhiteSpace(HardcoverPlugin.Instance?.Configuration.ApiToken);

    /// <summary>
    /// Searches for books whose title loosely matches <paramref name="title"/>.
    /// </summary>
    public async Task<IReadOnlyList<HardcoverBook>> SearchBooksAsync(string title, int limit, CancellationToken cancellationToken)
    {
        const string query = """
            query SearchBooks($title: String!, $limit: Int!) {
              books(
                where: { title: { _ilike: $title } }
                limit: $limit
                order_by: { users_count: desc }
              ) {
                id
                slug
                title
                release_date
                pages
                rating
                ratings_count
                users_count
                image { url }
                contributions {
                  contribution
                  author { id name }
                }
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["title"] = $"%{title}%",
            ["limit"] = limit
        };

        var data = await ExecuteAsync<BooksData>(query, variables, cancellationToken).ConfigureAwait(false);
        return data?.Books ?? (IReadOnlyList<HardcoverBook>)Array.Empty<HardcoverBook>();
    }

    /// <summary>
    /// Fetches full detail for a single book by its Hardcover numeric id.
    /// </summary>
    public async Task<HardcoverBook?> GetBookAsync(int hardcoverId, CancellationToken cancellationToken)
    {
        const string query = """
            query GetBook($id: Int!) {
              books_by_pk(id: $id) {
                id
                slug
                title
                description
                release_date
                pages
                rating
                ratings_count
                users_count
                image { url }
                cached_tags
                contributions {
                  contribution
                  author { id name }
                }
              }
            }
            """;

        var variables = new Dictionary<string, object?> { ["id"] = hardcoverId };

        var data = await ExecuteAsync<BookByPkData>(query, variables, cancellationToken).ConfigureAwait(false);
        return data?.Book;
    }

    /// <summary>
    /// Searches for authors whose name loosely matches <paramref name="name"/>.
    /// </summary>
    public async Task<IReadOnlyList<HardcoverAuthor>> SearchAuthorsAsync(string name, int limit, CancellationToken cancellationToken)
    {
        const string query = """
            query SearchAuthors($name: String!, $limit: Int!) {
              authors(
                where: { name: { _ilike: $name } }
                limit: $limit
                order_by: { books_count: desc }
              ) {
                id
                slug
                name
                image { url }
                books_count
              }
            }
            """;

        var variables = new Dictionary<string, object?>
        {
            ["name"] = $"%{name}%",
            ["limit"] = limit
        };

        var data = await ExecuteAsync<AuthorsData>(query, variables, cancellationToken).ConfigureAwait(false);
        return data?.Authors ?? (IReadOnlyList<HardcoverAuthor>)Array.Empty<HardcoverAuthor>();
    }

    /// <summary>
    /// Fetches full detail (bio, dates, image) for a single author by their Hardcover numeric id.
    /// </summary>
    public async Task<HardcoverAuthor?> GetAuthorAsync(int hardcoverId, CancellationToken cancellationToken)
    {
        const string query = """
            query GetAuthor($id: Int!) {
              authors_by_pk(id: $id) {
                id
                slug
                name
                bio
                born_date
                death_date
                books_count
                image { url }
              }
            }
            """;

        var variables = new Dictionary<string, object?> { ["id"] = hardcoverId };

        var data = await ExecuteAsync<AuthorByPkData>(query, variables, cancellationToken).ConfigureAwait(false);
        return data?.Author;
    }

    private async Task<TData?> ExecuteAsync<TData>(
        string query,
        Dictionary<string, object?> variables,
        CancellationToken cancellationToken)
    {
        var token = HardcoverPlugin.Instance?.Configuration.ApiToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Hardcover API token is not configured; skipping request. Set it under Dashboard -> Plugins -> Hardcover Metadata.");
            return default;
        }

        await ThrottleAsync(cancellationToken).ConfigureAwait(false);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, string.Empty)
        {
            Content = JsonContent.Create(new { query, variables }, options: SerializerOptions)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error calling Hardcover API");
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(
                "Hardcover API returned {StatusCode}: {Body}",
                (int)response.StatusCode,
                Truncate(body, 500));
            return default;
        }

        GraphQlResponse<TData>? parsed;
        try
        {
            parsed = await response.Content.ReadFromJsonAsync<GraphQlResponse<TData>>(SerializerOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Hardcover API response");
            return default;
        }

        if (parsed?.Errors is { Count: > 0 })
        {
            _logger.LogError(
                "Hardcover GraphQL error(s): {Errors}",
                string.Join("; ", parsed.Errors.Select(e => e.Message)));
            return default;
        }

        return parsed is null ? default : parsed.Data;
    }

    private static async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        await RateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var elapsed = DateTime.UtcNow - _lastCallUtc;
            if (elapsed < MinInterval)
            {
                await Task.Delay(MinInterval - elapsed, cancellationToken).ConfigureAwait(false);
            }

            _lastCallUtc = DateTime.UtcNow;
        }
        finally
        {
            RateGate.Release();
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
