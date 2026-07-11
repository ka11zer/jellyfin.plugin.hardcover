namespace Jellyfin.Plugin.Hardcover.ExternalIds;

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

public class HardcoverAuthorExternalId : IExternalId
{
    public string ProviderName => "Hardcover";

    public string Key => "HardcoverAuthorId";

    public string Type => MetadataType.Person;

    public string UrlFormatString => "https://hardcover.app/authors/{0}";

    public bool Supports(IHasLookupInfo item) => item is Person;
}
