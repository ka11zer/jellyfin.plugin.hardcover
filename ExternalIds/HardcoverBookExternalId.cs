namespace Jellyfin.Plugin.Hardcover.ExternalIds;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

public class HardcoverBookExternalId : IExternalId
{
    public string ProviderName => "Hardcover";

    public string Key => "HardcoverBookId";

    // Updated to use the ExternalIdMediaType enum
    public ExternalIdMediaType? Type => ExternalIdMediaType.Book;

    public string UrlFormatString => "https://hardcover.app/books/{0}";

    // Updated to expect IHasProviderIds
    public bool Supports(IHasProviderIds item) => item is Book;
}
