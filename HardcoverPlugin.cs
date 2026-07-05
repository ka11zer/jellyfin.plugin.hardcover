using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.Hardcover.Configuration;
using MediaBrowser.Controller.Providers;
using Jellyfin.Plugin.Hardcover.Providers;
using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Controller.Entities;
using Jellyfin.Plugin.Hardcover.Api;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hardcover;

public class HardcoverPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public HardcoverPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IServiceCollection services)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        
        // Register providers directly
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddSingleton<IMetadataProvider<Book>, HardcoverBookProvider>();
        services.AddSingleton<IMetadataProvider<Person>, HardcoverPersonProvider>();
        services.AddSingleton<IRemoteImageProvider, HardcoverBookImageProvider>();
    }

    public static HardcoverPlugin? Instance { get; private set; }

    public override string Name => "Hardcover";
    public override string Description => "Fetches metadata for books and authors from Hardcover.";
    public override Guid Id => Guid.Parse("a1b2c3d4-e5f6-7890-1234-567890abcdef"); // ← Use your actual plugin GUID!

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
