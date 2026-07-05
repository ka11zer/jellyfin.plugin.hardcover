using System;
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

public class HardcoverPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>, IHasOrder
{
    private readonly IHardcoverApiService _api;

    public HardcoverPersonProvider(ILoggerFactory loggerFactory)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _api = new HardcoverApiService(client, loggerFactory);
    }

    public string Name => "Hardcover";
    public int Order => 1;

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();
        var authors = await _api.SearchAuthorsAsync(searchInfo.Name, cancellationToken);
        foreach (var author in authors)
        {
            var result = new RemoteSearchResult
            {
                Name = author.Name,
                SearchProviderName = Name,
            };
            result.SetProviderId("Hardcover", author.Slug);
            results.Add(result);
        }
        return results;
    }

    public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Person>();
        string? existingId = null;
        info.ProviderIds.TryGetValue("Hardcover", out existingId);
        AuthorDetails? author = null;

        if (!string.IsNullOrEmpty(existingId))
            author = await _api.GetAuthorByIdAsync(existingId, cancellationToken);

        if (author == null)
        {
            var searchResults = await _api.SearchAuthorsAsync(info.Name, cancellationToken);
            var best = searchResults.FirstOrDefault();
            if (best != null)
                author = await _api.GetAuthorByIdAsync(best.Slug, cancellationToken);
        }

        if (author == null) return result;

        result.Item = new Person
        {
            Name = author.Name,
            Overview = string.IsNullOrWhiteSpace(author.Biography) ? null : author.Biography,
        };
        result.HasMetadata = true;
        result.Provider = Name;
        result.Item.ProviderIds["Hardcover"] = author.Slug;
        return result;
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
}
