using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.Hardcover.Configuration;

namespace Jellyfin.Plugin.Hardcover;

public class HardcoverPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public HardcoverPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static HardcoverPlugin? Instance { get; private set; }

    public override string Name => "Hardcover";
    public override string Description => "Fetches metadata for books and authors from Hardcover.";
    public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-1234-567890abcdef"); // ← use your actual plugin GUID!

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "Hardcover",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }
}
