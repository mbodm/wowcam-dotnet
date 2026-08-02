using System.Text.Json;
using WOWCAM.Parts.Core;

namespace WOWCAM.Parts.Addons.ApiClient;

public sealed class ApiClientDefault(IHttpClientProvider httpClientProvider) : IApiClient
{
    private readonly IHttpClientProvider httpClientProvider = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));

    public string ApiToken { get; set; } = "UNSET_API_TOKEN";

    public async Task<IEnumerable<ApiClientAddon>> GetAddonDownloadUrlsAsync(IEnumerable<string> addonNames, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addonNames);

        var httpClient = httpClientProvider.GetHttpClient();

        var cachedAddons = await GetCachedAddonsAsync(addonNames, ApiToken, httpClient, cancellationToken).ConfigureAwait(false);
        var namesOfCachedAddons = cachedAddons.Select(addonData => addonData.AddonSlug);
        var namesOfMissingAddons = addonNames.Except(namesOfCachedAddons);
        var uncachedAddons = await GetUncachedAddonsAsync(namesOfMissingAddons, ApiToken, httpClient, cancellationToken).ConfigureAwait(false);

        return cachedAddons.Concat(uncachedAddons).DistinctBy(addonData => addonData.AddonSlug);
    }

    private static async Task<IEnumerable<ApiClientAddon>> GetCachedAddonsAsync(IEnumerable<string> addonNames, string token, HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ApiClientAddon>();

        var url = $"https://wowcam.mbodm.deno.net/cache/show?token={token}";
        using var response = await httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var entries = json.RootElement.GetProperty("entries");
        foreach (var e in entries.EnumerateArray())
        {
            var addonSlug = e.GetProperty("addonSlug").GetString();
            var downloadUrl = e.GetProperty("downloadUrl").GetString();

            if (string.IsNullOrWhiteSpace(addonSlug) || string.IsNullOrWhiteSpace(downloadUrl))
            {
                continue;
            }

            // Just to be 100% sure the API returned the correct addon before adding it
            if (!addonNames.Any(addonName => addonName.Equals(addonSlug, StringComparison.CurrentCultureIgnoreCase)))
            {
                continue;
            }

            result.Add(new ApiClientAddon(addonSlug, downloadUrl));
        }

        return result;
    }

    private static async Task<IEnumerable<ApiClientAddon>> GetUncachedAddonsAsync(IEnumerable<string> addonNames, string token, HttpClient httpClient,
        CancellationToken cancellationToken = default)
    {
        var result = new List<ApiClientAddon>();

        var url = $"https://wowcam.mbodm.deno.net/get?token={token}&addon=";

        foreach (var addonName in addonNames)
        {
            using var response = await httpClient.GetAsync(url + addonName.ToLower(), cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!json.RootElement.TryGetProperty("addonSlug", out JsonElement addonSlugElement) ||
                !json.RootElement.TryGetProperty("downloadUrl", out JsonElement downloadUrlElement))
            {
                continue;
            }

            var addonSlug = addonSlugElement.GetString();
            var downloadUrl = downloadUrlElement.GetString();
            if (string.IsNullOrWhiteSpace(addonSlug) || string.IsNullOrWhiteSpace(downloadUrl))
            {
                continue;
            }

            // Just to be 100% sure the API returned the correct addon before adding it
            if (!addonName.Equals(addonSlug, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            result.Add(new ApiClientAddon(addonSlug, downloadUrl));
        }

        return result;
    }
}
