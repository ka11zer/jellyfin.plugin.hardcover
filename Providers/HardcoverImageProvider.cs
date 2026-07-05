using System;
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

/// <summary>
/// Supplies cover art for books and profile photos for authors, sourced from whichever
/// Hardcover record is already linked to the item via <see cref="HardcoverBookProvider"/>
/// or <see cref="HardcoverPersonProvider"/>.
/// </summary>
public class HardcoverImageProvider : IRemoteImageProvider
{
    private readonly HardcoverApiClient _api;
    private readonly ILogger<HardcoverImageProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardcoverImageProvider"/> class.
    /// </summary>
    public HardcoverImageProvider(HardcoverApiClient api, IHttpClientFactory httpClientFactory, ILogger<HardcoverImageProvider> logger)
    {
        _api = api;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Hardcover";

    /// <inheritdoc />
    public bool Supports(BaseItem item) => item is Book || item is Person;

    /// <inheritdoc />
    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        yield return ImageType.Primary;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!HardcoverApiClient.HasApiToken)
        {
            return Array.Empty<RemoteImageInfo>();
        }

        var providerId = item.GetProviderId(HardcoverBookProvider.ProviderName);
        if (string.IsNullOrEmpty(providerId) || !int.TryParse(providerId, out var id))
        {
            return Array.Empty<RemoteImageInfo>();
        }

        string? imageUrl = item switch
        {
            Book => (await _api.GetBookAsync(id, cancellationToken).ConfigureAwait(false))?.Image?.Url,
            Person => (await _api.GetAuthorAsync(id, cancellationToken).ConfigureAwait(false))?.Image?.Url,
            _ => null
        };

        if (string.IsNullOrEmpty(imageUrl))
        {
            return Array.Empty<RemoteImageInfo>();
        }

        return new[]
        {
            new RemoteImageInfo
            {
                ProviderName = Name,
                Type = ImageType.Primary,
                Url = imageUrl
            }
        };
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        // Hardcover's asset CDN serves cover art/photos without needing the API token.
        var client = _httpClientFactory.CreateClient(HardcoverApiClient.HttpClientName);
        return client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }
}
