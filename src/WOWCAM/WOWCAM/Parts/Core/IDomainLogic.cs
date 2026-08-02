using WOWCAM.Parts.Config;

namespace WOWCAM.Parts.Core;

public interface IDomainLogic
{
    Task<ConfigData> LoadConfigAsync(CancellationToken cancellationToken = default);
    HttpClient CreateHttpClient();
    Task<string> InitAsync(string workFolder, CancellationToken cancellationToken = default);
    Task DeployAddonsAsync(string deployFolder, string targetFolder, CancellationToken cancellationToken = default);
    void CleanUp(string tempFolder);
    Task<uint> ProcessAddonsAsync(Action<IEnumerable<string>>? preProgress = null, IProgress<byte>? progress = null, CancellationToken cancellationToken = default);
}
