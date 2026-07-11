namespace Jellyfin.Plugin.Hardcover.Providers;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

public class HardcoverAuthorProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HardcoverAuthorProvider> _logger;

    public HardcoverAuthorProvider(IHttpClientFactory httpClientFactory, ILogger<HardcoverAuthorProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "Hardcover";

    public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Person> { HasMetadata = false };
        // Your GraphQL author fetching implementation goes here
        return await Task.FromResult(result);
    }

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();
        // Your GraphQL author search implementation goes here
        return await Task.FromResult(results);
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        return client.GetAsync(new Uri(url), cancellationToken);
    }
}
