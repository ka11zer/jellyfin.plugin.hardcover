using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hardcover.Api;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hardcover.Providers;

public class HardcoverBookImageProvider : IRemoteImageProvider
{
    private readonly IHardcoverApiService _api;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HardcoverBookImageProvider> _logger;

    public HardcoverBookImageProvider(IHardcoverApiService api, IHttpClientFactory httpClientFactory, ILogger<HardcoverBookImageProvider> logger)
    {
        _api = api;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => "Hardcover";
    public bool Supports(BaseItem item) => item is Book;  // or Person if we add author images

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item) => new[] { ImageType.Primary };

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        var list = new List<RemoteImageInfo>();
        var slug = item.ProviderIds.GetOrDefault("Hardcover");
        if (string.IsNullOrEmpty(slug))
            yield break;

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
        var client = _httpClientFactory.CreateClient();
        return client.GetAsync(url, cancellationToken);
    }
}
