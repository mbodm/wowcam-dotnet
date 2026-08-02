namespace WOWCAM.Parts.Addons.ApiClient;

public interface IApiClient
{
    string ApiToken { get; set; }

    Task<IEnumerable<ApiClientAddon>> GetAddonDownloadUrlsAsync(IEnumerable<string> addonSlugs, CancellationToken cancellationToken = default);
}
