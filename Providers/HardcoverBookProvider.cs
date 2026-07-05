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

public class HardcoverBookProvider : IRemoteMetadataProvider<Book, BookInfo>, IHasOrder
{
    private readonly IHardcoverApiService _api;

    public HardcoverBookProvider(ILoggerFactory loggerFactory)
    {
        var client = new HttpClient 
        { 
            Timeout = TimeSpan.FromSeconds(30),
            BaseAddress = new Uri("https://api.hardcover.app/v1/")
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin.Hardcover/1.0");
        _api = new HardcoverApiService(client, loggerFactory);
    }

    public string Name => "Hardcover";
    public int Order => 0; // Higher priority than ComicVine

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo searchInfo, CancellationToken cancellationToken)
    {
        var results = new List<RemoteSearchResult>();
        
        if (string.IsNullOrWhiteSpace(searchInfo.Name))
            return results;

        var books = await _api.SearchBooksAsync(searchInfo.Name, cancellationToken);
        
        foreach (var book in books)
        {
            var result = new RemoteSearchResult
            {
                Name = book.Title,
                SearchProviderName = Name,
                Overview = book.AuthorName != null ? $"By {book.AuthorName}" : null,
            };
            result.SetProviderId("Hardcover", book.Slug);
            results.Add(result);
        }
        
        return results;
    }

    public async Task<MetadataResult<Book>> GetMetadata(BookInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Book>();
        string? existingId = null;
        info.ProviderIds.TryGetValue("Hardcover", out existingId);
        BookDetails? book = null;

        if (!string.IsNullOrEmpty(existingId))
            book = await _api.GetBookByIdAsync(existingId, cancellationToken);

        if (book == null)
        {
            var searchResults = await _api.SearchBooksAsync(info.Name, cancellationToken);
            var best = searchResults.FirstOrDefault();
            if (best != null)
                book = await _api.GetBookByIdAsync(best.Slug, cancellationToken);
        }

        if (book == null) return result;

        result.Item = new Book
        {
            Name = book.Title,
            Overview = book.Description,
            ProductionYear = book.PublicationYear,
        };

        result.Item.ProviderIds["Hardcover"] = book.Slug;

        if (!string.IsNullOrEmpty(book.Publisher))
            result.Item.Studios = new[] { book.Publisher };

        result.HasMetadata = true;
        result.Provider = Name;

        return result;
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
}
