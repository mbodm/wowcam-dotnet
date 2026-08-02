namespace WOWCAM.Parts.Addons.SmartUpdate;

public interface ISmartUpdate
{
    Task<int> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    
    bool AddonExists(string addonName, string downloadUrl);
    void AddOrUpdateAddon(string addonName, string downloadUrl);
    Task DeployAddonAsync(string addonName, string destFolder);
}
