using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Hardcover.Configuration;

/// <summary>
/// Configuration for the Hardcover metadata plugin, editable from the Jellyfin dashboard.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets the Hardcover Personal API Token.
    /// Generate one at https://hardcover.app/account/api.
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether search results should be biased
    /// toward the most-added/most-read book on Hardcover when several matches
    /// share a similar title. Helps disambiguate common titles.
    /// </summary>
    public bool PreferPopularMatches { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of candidate results to request from
    /// Hardcover when searching for a book or author match.
    /// </summary>
    public int MaxSearchResults { get; set; } = 10;
}
