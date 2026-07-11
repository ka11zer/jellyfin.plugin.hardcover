namespace Jellyfin.Plugin.Hardcover.ExternalIds;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

public class HardcoverAuthorExternalId : IExternalId
{
    public string ProviderName => "Hardcover";

    public string Key => "HardcoverAuthorId";

    // Updated to use the ExternalIdMediaType enum
    public ExternalIdMediaType? Type => ExternalIdMediaType.Person;

    public string UrlFormatString => "https://hardcover.app/authors/{0}";

    // Updated to expect IHasProviderIds
    public bool Supports(IHasProviderIds item) => item is Person;
}
