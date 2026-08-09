namespace WOWCAM.Core.Parts.Addons.ApiClient;

internal interface IApiClient
{
    string ApiToken { get; set; }

    Task<IEnumerable<ApiClientAddon>> GetAddonDownloadUrlsAsync(IEnumerable<string> addonSlugs, CancellationToken cancellationToken = default);
}
