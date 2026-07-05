using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hardcover.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hardcover.Providers;

public class HardcoverPersonProvider : IRemoteMetadataProvider<Person, PersonInfo>, IHasOrder
{
    private readonly IHardcoverApiService _api;
    private readonly ILogger<HardcoverPersonProvider> _logger;

    public HardcoverPersonProvider(IHardcoverApiService api, ILogger<HardcoverPersonProvider> logger)
    {
        _api = api;
        _logger = logger;
    }

    public string Name => "Hardcover";
    public int Order => 1; // After default providers, before others

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();
        var authors = await _api.SearchAuthorsAsync(searchInfo.Name, cancellationToken);

        foreach (var author in authors)
        {
            var result = new RemoteSearchResult
            {
                Name = author.Name,
                SearchProviderName = Name,
                ImageUrl = null, // will be filled if we had a search image
            };
            result.SetProviderId("Hardcover", author.Slug);
            results.Add(result);
        }
        return results;
    }

    public async Task<MetadataResult<Person>> GetMetadata(PersonInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Person>();

        // Try to find existing Hardcover ID
        var existingId = info.ProviderIds.GetOrDefault("Hardcover");
        AuthorDetails? author = null;

        if (!string.IsNullOrEmpty(existingId))
        {
            author = await _api.GetAuthorByIdAsync(existingId, cancellationToken);
        }

        if (author == null)
        {
            // Search by name and pick first match
            var searchResults = await _api.SearchAuthorsAsync(info.Name, cancellationToken);
            var best = searchResults.FirstOrDefault();
            if (best != null)
                author = await _api.GetAuthorByIdAsync(best.Slug, cancellationToken);
        }

        if (author == null)
            return result;

        result.Item = new Person
        {
            Name = author.Name,
            Overview = string.IsNullOrWhiteSpace(author.Biography) ? null : author.Biography,
        };

        result.HasMetadata = true;
        result.Provider = Name;

        // Store provider ID
        result.Item.ProviderIds["Hardcover"] = author.Slug;

        // If author has an image URL, we could set it here, but better to use an image provider.
        // We'll let the image provider handle covers.

        return result;
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
}
