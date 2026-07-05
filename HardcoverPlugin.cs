using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Hardcover.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Hardcover;

/// <summary>
/// The main plugin entry point registered with the Jellyfin server.
/// </summary>
public class HardcoverPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    // NOTE: this GUID must be unique to this plugin and must never change between releases,
    // otherwise Jellyfin will treat upgrades as a brand-new plugin install.
    private static readonly Guid PluginId = Guid.Parse("a1b2c3d4-e5f6-7890-1234-567890abcdef");

    /// <summary>
    /// Initializes a new instance of the <see cref="HardcoverPlugin"/> class.
    /// </summary>
    public HardcoverPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the running instance of the plugin, used by providers to reach the current configuration.
    /// </summary>
    public static HardcoverPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Hardcover Metadata";

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <inheritdoc />
    public override string Description => "Fetches book and author metadata from Hardcover (hardcover.app).";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
        };
    }
}
