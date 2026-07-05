using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Hardcover.Api;
using Jellyfin.Plugin.Hardcover.Api.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hardcover.Providers;

/// <summary>
/// Supplies book metadata (title, overview, release date, rating, authors) from Hardcover.
/// </summary>
public class HardcoverBookProvider : IRemoteMetadataProvider<Book, BookInfo>
{
    /// <summary>
    /// The provider id key used to stash the Hardcover book id on library items,
    /// and to look it up again on subsequent refreshes without a fresh search.
    /// </summary>
    public const string ProviderName = "Hardcover";

    private readonly HardcoverApiClient _api;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HardcoverBookProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardcoverBookProvider"/> class.
    /// </summary>
    public HardcoverBookProvider(HardcoverApiClient api, IHttpClientFactory httpClientFactory, ILogger<HardcoverBookProvider> logger)
    {
        _api = api;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => ProviderName;

    /// <inheritdoc />
    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BookInfo searchInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchInfo.Name) || !HardcoverApiClient.HasApiToken)
        {
            return Enumerable.Empty<RemoteSearchResult>();
        }

        var maxResults = HardcoverPlugin.Instance?.Configuration.MaxSearchResults ?? 10;
        var books = await _api.SearchBooksAsync(searchInfo.Name, maxResults, cancellationToken).ConfigureAwait(false);

        // If the folder/file name also implies a year, prefer matches close to it, but never
        // drop every candidate just because the year is missing or slightly off.
        if (searchInfo.Year.HasValue)
        {
            var withYear = books.Where(b => TryGetYear(b.ReleaseDate) == searchInfo.Year).ToList();
            if (withYear.Count > 0)
            {
                books = withYear;
            }
        }

        return books.Select(ToSearchResult);
    }

    /// <inheritdoc />
    public async Task<MetadataResult<Book>> GetMetadata(BookInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Book> { HasMetadata = false };

        if (!HardcoverApiClient.HasApiToken)
        {
            return result;
        }

        HardcoverBook? book = null;

        // Reuse the id we already matched during a previous search/refresh, if we have one.
        if (info.ProviderIds.TryGetValue(ProviderName, out var idString) && int.TryParse(idString, out var id))
        {
            book = await _api.GetBookAsync(id, cancellationToken).ConfigureAwait(false);
        }

        // Otherwise fall back to a fresh title search and take the best (most-read) hit.
        if (book is null && !string.IsNullOrWhiteSpace(info.Name))
        {
            var maxResults = HardcoverPlugin.Instance?.Configuration.MaxSearchResults ?? 10;
            var candidates = await _api.SearchBooksAsync(info.Name, maxResults, cancellationToken).ConfigureAwait(false);
            var best = PickBestCandidate(candidates, info);
            if (best is not null)
            {
                book = await _api.GetBookAsync(best.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        if (book is null)
        {
            return result;
        }

        var item = new Book
        {
            Name = book.Title,
            Overview = CleanDescription(book.Description)
        };

        if (TryGetYear(book.ReleaseDate) is int year)
        {
            item.ProductionYear = year;
        }

        if (DateTime.TryParse(book.ReleaseDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var releaseDate))
        {
            item.PremiereDate = releaseDate;
        }

        if (book.Rating is double rating)
        {
            item.CommunityRating = (float)rating;
        }

        item.SetProviderId(ProviderName, book.Id.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrEmpty(book.Slug))
        {
            item.HomePageUrl = $"https://hardcover.app/books/{book.Slug}";
        }

        result.Item = item;
        result.HasMetadata = true;

        // Attach authors/translators/etc. as People so Jellyfin can display them and so the
        // companion HardcoverPersonProvider can enrich each one on its own refresh pass.
        foreach (var person in BuildPeople(book))
        {
            result.AddPerson(person);
        }

        return result;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        // Hardcover's asset CDN serves cover art without needing the API token.
        var client = _httpClientFactory.CreateClient(HardcoverApiClient.HttpClientName);
        return client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static IEnumerable<PersonInfo> BuildPeople(HardcoverBook book)
    {
        if (book.Contributions is null)
        {
            yield break;
        }

        foreach (var contribution in book.Contributions)
        {
            if (contribution.Author?.Name is null)
            {
                continue;
            }

            var person = new PersonInfo
            {
                Name = contribution.Author.Name,
                Type = MapContributionToPersonKind(contribution.Contribution)
            };

            person.SetProviderId(HardcoverPersonProvider.ProviderName, contribution.Author.Id.ToString(CultureInfo.InvariantCulture));
            yield return person;
        }
    }

    private static PersonKind MapContributionToPersonKind(string? contribution)
    {
        // Hardcover leaves "contribution" null/empty for the primary author and fills it in
        // for anything else (Translator, Illustrator, Editor, etc.).
        if (string.IsNullOrWhiteSpace(contribution))
        {
            return PersonKind.Author;
        }

        return contribution.Trim().ToLowerInvariant() switch
        {
            "translator" => PersonKind.Translator,
            "illustrator" => PersonKind.Illustrator,
            "editor" => PersonKind.Editor,
            _ => PersonKind.Author
        };
    }

    private HardcoverBook? PickBestCandidate(IReadOnlyList<HardcoverBook> candidates, BookInfo info)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var preferPopular = HardcoverPlugin.Instance?.Configuration.PreferPopularMatches ?? true;
        IEnumerable<HardcoverBook> scored = candidates;

        if (info.Year.HasValue)
        {
            var sameYear = candidates.Where(b => TryGetYear(b.ReleaseDate) == info.Year).ToList();
            if (sameYear.Count > 0)
            {
                scored = sameYear;
            }
        }

        return preferPopular
            ? scored.OrderByDescending(b => b.UsersCount ?? 0).FirstOrDefault()
            : scored.FirstOrDefault();
    }

    private static RemoteSearchResult ToSearchResult(HardcoverBook book)
    {
        var result = new RemoteSearchResult
        {
            Name = book.Title,
            SearchProviderName = ProviderName,
            ImageUrl = book.Image?.Url
        };

        result.SetProviderId(ProviderName, book.Id.ToString(CultureInfo.InvariantCulture));

        if (TryGetYear(book.ReleaseDate) is int year)
        {
            result.ProductionYear = year;
        }

        var primaryAuthor = book.Contributions?
            .FirstOrDefault(c => string.IsNullOrWhiteSpace(c.Contribution))?
            .Author?.Name;

        if (!string.IsNullOrEmpty(primaryAuthor))
        {
            result.Overview = $"by {primaryAuthor}";
        }

        return result;
    }

    private static int? TryGetYear(string? releaseDate)
    {
        if (!string.IsNullOrEmpty(releaseDate) &&
            releaseDate.Length >= 4 &&
            int.TryParse(releaseDate.AsSpan(0, 4), out var year))
        {
            return year;
        }

        return null;
    }

    private static string? CleanDescription(string? description)
    {
        // Hardcover book descriptions occasionally include basic HTML (<p>, <br>); strip it
        // so it renders cleanly in Jellyfin's plain-text overview field.
        if (string.IsNullOrEmpty(description))
        {
            return description;
        }

        return System.Text.RegularExpressions.Regex.Replace(description, "<[^>]+>", string.Empty).Trim();
    }
}
