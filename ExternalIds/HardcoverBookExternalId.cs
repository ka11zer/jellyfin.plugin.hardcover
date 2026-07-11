namespace Jellyfin.Plugin.Hardcover.ExternalIds;

using MediaBrowser.Controller.Entities.Books;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

public class HardcoverBookExternalId : IExternalId
{
    public string ProviderName => "Hardcover";

    public string Key => "HardcoverBookId";

    public string Type => MetadataType.Book;

    public string UrlFormatString => "https://hardcover.app/books/{0}";

    public bool Supports(IHasLookupInfo item) => item is Book;
}
