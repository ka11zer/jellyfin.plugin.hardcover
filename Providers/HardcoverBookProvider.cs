using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hardcover.Api;
using Jellyfin.Plugin.Hardcover.Api.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hardcover.Providers;

/// <summary>
/// Supplies author metadata (bio, birth/death dates, photo) from Hardcover for Person
/// entries linked to books (e.g. via <see cref="HardcoverBookProvider"/>'s contributions).
/// </summary>
public class HardcoverPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
{
    /// <summary>
    /// The provider id key used to stash the Hardcover author id on Person items.
    /// </summary>
    public const string ProviderName = "Hardcover";

    private readonly HardcoverApiClient _api;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HardcoverPersonProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardcoverPersonProvider"/> class.
    /// </summary>
    public HardcoverPersonProvider(HardcoverApiClient api, IHttpClientFactory httpClientFactory, ILogger<HardcoverPersonProvider> logger)
    {
        _api = api;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchInfo.Name) || !HardcoverApiClient.HasApiToken)
        {
            return Enumerable.Empty<RemoteSearchResult>();
        }

        var maxResults = HardcoverPlugin.Instance?.Configuration.MaxSearchResults ?? 10;
        var authors = await _api.SearchAuthorsAsync(searchInfo.Name, maxResults, cancellationToken).ConfigureAwait(false);

        return authors.Select(author => new RemoteSearchResult
        {
            Name = author.Name,
            SearchProviderName = ProviderName,
            ImageUrl = author.Image?.Url,
            ProviderIds = { [ProviderName] = author.Id.ToString(CultureInfo.InvariantCulture) }
        });
    }

    /// <inheritdoc />
    public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Person> { HasMetadata = false };

        if (!HardcoverApiClient.HasApiToken)
        {
            return result;
        }

        HardcoverAuthor? author = null;

        if (info.ProviderIds.TryGetValue(ProviderName, out var idString) && int.TryParse(idString, out var id))
        {
            author = await _api.GetAuthorAsync(id, cancellationToken).ConfigureAwait(false);
        }

        if (author is null && !string.IsNullOrWhiteSpace(info.Name))
        {
            var maxResults = HardcoverPlugin.Instance?.Configuration.MaxSearchResults ?? 10;
            var candidates = await _api.SearchAuthorsAsync(info.Name, maxResults, cancellationToken).ConfigureAwait(false);

            // Prefer an exact (case-insensitive) name match over the most-prolific author
            // sharing a substring of the name.
            var best = candidates.FirstOrDefault(a => string.Equals(a.Name, info.Name, StringComparison.OrdinalIgnoreCase))
                       ?? candidates.OrderByDescending(a => a.BooksCount ?? 0).FirstOrDefault();

            if (best is not null)
            {
                author = await _api.GetAuthorAsync(best.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        if (author is null)
        {
            return result;
        }

        var person = new Person
        {
            Name = author.Name,
            Overview = author.Bio
        };

        if (DateTime.TryParse(author.BornDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var born))
        {
            person.PremiereDate = born;
        }

        if (DateTime.TryParse(author.DeathDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var died))
        {
            person.EndDate = died;
        }

        person.SetProviderId(ProviderName, author.Id.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(author.Slug))
        {
            person.HomePageUrl = $"https://hardcover.app/authors/{author.Slug}";
        }

        result.Item = person;
        result.HasMetadata = true;
        return result;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        // Hardcover's asset CDN serves author photos without needing the API token.
        var client = _httpClientFactory.CreateClient(HardcoverApiClient.HttpClientName);
        return client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }
}
