namespace WOWCAM.Parts.Addons.Curse;

internal interface IServerFetch
{
    Task DownloadAddonAsync(string downloadUrl, string zipFilePath, IProgress<byte>? downloadProgress = default, CancellationToken cancellationToken = default);
    Task<string> UnzipAddonAsync(string zipFilePath, string unzipFolder, CancellationToken cancellationToken = default);
}
