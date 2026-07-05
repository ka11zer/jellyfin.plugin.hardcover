using Jellyfin.Plugin.Hardcover.Api;
using Jellyfin.Plugin.Hardcover.Providers;
using MediaBrowser.Common.Plugins;          // <-- This is the missing using
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Hardcover;

public class HardcoverPluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddHttpClient<IHardcoverApiService, HardcoverApiService>(client =>
        {
            client.BaseAddress = new Uri("https://api.hardcover.app/v1/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin.Hardcover/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Providers for Books
        services.AddSingleton<IMetadataProvider<Book>, HardcoverBookProvider>();
        services.AddSingleton<IRemoteImageProvider, HardcoverBookImageProvider>();

        // Providers for Authors (Person)
        services.AddSingleton<IMetadataProvider<Person>, HardcoverPersonProvider>();
    }
}
