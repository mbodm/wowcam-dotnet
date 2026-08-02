namespace WOWCAM.Parts.Config;

public interface IConfigReader
{
    Task<ConfigData> ReadAsync(CancellationToken cancellationToken = default);
}
