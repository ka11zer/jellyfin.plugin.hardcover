namespace Jellyfin.Plugin.Hardcover.Configuration;

using MediaBrowser.Model.Plugins;

public class PluginConfiguration : BasePluginConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
}
