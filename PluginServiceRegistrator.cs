using Jellyfin.Plugin.Hardcover.Api;
using Jellyfin.Plugin.Hardcover.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Hardcover;

public class HardcoverPluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection services)
    {
        // Register the API service with an HttpClient
        services.AddHttpClient<IHardcoverApiService, HardcoverApiService>(client =>
        {
            client.BaseAddress = new Uri("https://api.hardcover.app/v1/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin.Hardcover/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register metadata providers
        services.AddSingleton<IMetadataProvider<Person>, HardcoverPersonProvider>();
        services.AddSingleton<IMetadataProvider<Book>, HardcoverBookProvider>();

        // Register remote image provider for books
        services.AddSingleton<IRemoteImageProvider, HardcoverBookImageProvider>();
    }
}
