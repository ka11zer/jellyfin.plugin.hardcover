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

public class HardcoverBookProvider : IRemoteMetadataProvider<Book, BookInfo>, IHasOrder
{
    private readonly IHardcoverApiService _api;
    private readonly ILogger<HardcoverBookProvider> _logger;

    public HardcoverBookProvider(IHardcoverApiService api, ILogger<HardcoverBookProvider> logger)
    {
        _api = api;
        _logger = logger;
    }

    public string Name => "Hardcover";
    public int Order => 1;

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();
        var books = await _api.SearchBooksAsync(searchInfo.Name, cancellationToken);

        foreach (var book in books)
        {
            var result = new RemoteSearchResult
            {
                Name = book.Title,
                SearchProviderName = Name,
                PremiereDate = null, // we may fill later
            };
            result.SetProviderId("Hardcover", book.Slug);
            results.Add(result);
        }
        return results;
    }

    public async Task<MetadataResult<Book>> GetMetadata(BookInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Book>();
        var existingId = info.ProviderIds.GetOrDefault("Hardcover");
        BookDetails? book = null;

        if (!string.IsNullOrEmpty(existingId))
        {
            book = await _api.GetBookByIdAsync(existingId, cancellationToken);
        }

        if (book == null)
        {
            var searchResults = await _api.SearchBooksAsync(info.Name, cancellationToken);
            var best = searchResults.FirstOrDefault();
            if (best != null)
                book = await _api.GetBookByIdAsync(best.Slug, cancellationToken);
        }

        if (book == null)
            return result;

        result.Item = new Book
        {
            Name = book.Title,
            Overview = book.Description,
            ProductionYear = book.PublicationYear,
        };

        // Add authors as people (links to Person library)
        if (book.Authors?.Any() == true)
        {
            // We only get names; we could optionally search for them and set IDs.
            result.Item.AddPerson(CreatePersonInfo(book.Authors));
        }

        result.HasMetadata = true;
        result.Provider = Name;
        result.Item.ProviderIds["Hardcover"] = book.Slug;

        // Store publisher in studio?
        if (!string.IsNullOrEmpty(book.Publisher))
            result.Item.Studios = new[] { book.Publisher };

        return result;
    }

    private static PersonInfo[] CreatePersonInfo(List<string> authors)
    {
        return authors.Select(a => new PersonInfo
        {
            Name = a,
            Type = PersonKind.Author
        }).ToArray();
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
}
