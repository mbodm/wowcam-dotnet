namespace WOWCAM.Core.Parts.Addons.SmartUpdate;

internal sealed class SmartUpdateDefault : ISmartUpdate
{
    public bool AddonExists(string addonName, string downloadUrl)
    {
        return false;
    }

    public void AddOrUpdateAddon(string addonName, string downloadUrl)
    {
        throw new NotImplementedException();
    }

    public Task DeployAddonAsync(string addonName, string destFolder)
    {
        throw new NotImplementedException();
    }

    public Task<int> LoadAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
