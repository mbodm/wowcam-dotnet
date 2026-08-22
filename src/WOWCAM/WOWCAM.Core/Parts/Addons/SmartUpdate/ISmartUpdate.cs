namespace WOWCAM.Core.Parts.Addons.SmartUpdate;

internal interface ISmartUpdate
{
    Task<int> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);

    Task CacheAddonAsync(string addonName, string downloadUrl, string unzippedAddonContentFolder, CancellationToken cancellationToken = default);
    Task<bool> AddonAlreadyCachedAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default);
    Task DeployCachedAddonAsync(string addonName, string destFolder, CancellationToken cancellationToken = default);
}
