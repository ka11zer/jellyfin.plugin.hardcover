namespace Jellyfin.Plugin.Hardcover.Configuration;

using MediaBrowser.Model.Plugins;

public class PluginConfiguration : BasePluginConfiguration
{
    public string HardcoverApiKey { get; set; } = string.Empty;
}
