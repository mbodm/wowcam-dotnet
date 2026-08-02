namespace WOWCAM.Parts.Addons.SmartUpdate;

public sealed class SmartUpdateDefault : ISmartUpdate
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

    public Task<ushort> LoadAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
