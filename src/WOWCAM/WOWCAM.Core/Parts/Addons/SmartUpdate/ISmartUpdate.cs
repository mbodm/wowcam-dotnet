namespace WOWCAM.Core.Parts.Addons.SmartUpdate;

internal interface ISmartUpdate
{
    Task<int> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);

    Task<bool> AddonAlreadyExistsAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default);
    Task DeployExistingAddonAsync(string addonName, string destFolder, CancellationToken cancellationToken = default);
    Task AddOrUpdateEntryAsync(string addonName, string downloadUrl, string unzippedAddonSourceFolder, CancellationToken cancellationToken = default);
}
