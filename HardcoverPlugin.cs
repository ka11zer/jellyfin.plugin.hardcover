namespace Jellyfin.Plugin.Hardcover;

using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Hardcover.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

public class HardcoverPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public HardcoverPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "Hardcover Book Metadata";

    // Static GUID generated specifically for your Hardcover implementation
    public override Guid Id => Guid.Parse("d6b8f36c-94df-4fa3-94df-58076612df21");

    public static HardcoverPlugin? Instance { get; private set; }

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "Hardcover",
                EmbeddedResourcePath = string.Format("{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }
}
