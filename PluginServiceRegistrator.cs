using System;
using Jellyfin.Plugin.Hardcover.Api;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Hardcover;

/// <summary>
/// Registers plugin services (a named <see cref="System.Net.Http.HttpClient"/> for talking
/// to the Hardcover GraphQL API) with Jellyfin's dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddHttpClient(HardcoverApiClient.HttpClientName, client =>
        {
            client.BaseAddress = new Uri(HardcoverApiClient.GraphQlEndpoint);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Jellyfin-Hardcover-Plugin/1.0 (+https://github.com/)");
        });

        serviceCollection.AddSingleton<HardcoverApiClient>();
    }
}
