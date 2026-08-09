using WOWCAM.Helper;

namespace WOWCAM.Update;

internal interface IUpdateManager
{
    Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task DownloadUpdateAsync(string workFolder, UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = default, CancellationToken cancellationToken = default);
    Task ApplyUpdateAsync(CancellationToken cancellationToken = default);
    void RestartApplication(uint delayInSeconds);
    Task RemoveBakFileIfExistsAsync(CancellationToken cancellationToken = default);
}
