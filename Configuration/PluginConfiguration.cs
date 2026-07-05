using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Hardcover.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
}
