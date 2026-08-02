namespace WOWCAM.Parts.Config;

public interface IConfigReader
{
    string StorageInformation { get; } // Using such a generic term here since this could be a file/database/whatever

    Task<ConfigData> ReadAsync(CancellationToken cancellationToken = default);
}
