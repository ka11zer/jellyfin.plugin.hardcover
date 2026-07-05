using System.Collections.Generic;
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

public class HardcoverBookImageProvider : IRemoteImageProvider
{
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly IHardcoverApiService _api;

    public HardcoverBookImageProvider(ILogger<HardcoverBookImageProvider> logger)
    {
        _api = new HardcoverApiService(_httpClient, logger);
    }

    public string Name => "Hardcover";
    public bool Supports(BaseItem item) => item is Book;

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new[] { ImageType.Primary };

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var list = new List<RemoteImageInfo>();
        var slug = item.ProviderIds.GetOrDefault("Hardcover");
        if (string.IsNullOrEmpty(slug)) return list;

        var covers = await _api.GetBookCoverUrlsAsync(slug, cancellationToken);
        foreach (var url in covers)
        {
            list.Add(new RemoteImageInfo
            {
                ProviderName = Name,
                Url = url,
                Type = ImageType.Primary
            });
        }
        return list;
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        var client = new HttpClient(); // or reuse static _httpClient
        return client.GetAsync(url, cancellationToken);
    }
}
