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

public class HardcoverBookProvider : IRemoteMetadataProvider<Book, BookInfo>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HardcoverBookProvider> _logger;

    public HardcoverBookProvider(IHttpClientFactory httpClientFactory, ILogger<HardcoverBookProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "Hardcover";

    public async Task<MetadataResult<Book>> GetMetadata(BookInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Book> { HasMetadata = false };
        
        // This is the critical line that fixes your CS1061 error
        var apiKey = HardcoverPlugin.Instance?.Configuration.ApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Skipping metadata resolution: Missing Hardcover API Key.");
            return result;
        }

        // Your GraphQL book fetching implementation goes here
        return await Task.FromResult(result);
    }

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();
        // Your GraphQL book search implementation goes here
        return await Task.FromResult(results);
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        return client.GetAsync(new Uri(url), cancellationToken);
    }
}
