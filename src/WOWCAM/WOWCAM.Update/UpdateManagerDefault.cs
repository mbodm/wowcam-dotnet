using System.Diagnostics;
using WOWCAM.Helper;

namespace WOWCAM.Update;

internal sealed class UpdateManagerDefault(IGitHubClient gitHubClient, HttpClient httpClient) : IUpdateManager
{
    private readonly IGitHubClient gitHubClient = gitHubClient ?? throw new ArgumentNullException(nameof(gitHubClient));
    private readonly HttpClient httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    private string updateFolder = string.Empty;

    public async Task<UpdateData> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        Version installedVersion = GetInstalledVersion();
        GitHubReleaseData latestReleaseData = await gitHubClient.GetLatestReleaseDataAsync("mbodm", "wowcam", cancellationToken).ConfigureAwait(false);
        bool updateAvailable = installedVersion < latestReleaseData.Version;

        return new UpdateData(installedVersion, latestReleaseData.Version, updateAvailable, latestReleaseData.DownloadUrl, latestReleaseData.FileName);
    }

    public async Task DownloadUpdateAsync(string workFolder, UpdateData updateData, IProgress<DownloadProgress>? downloadProgress = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workFolder);
        ArgumentNullException.ThrowIfNull(updateData);

        var tempFolder = Path.Combine(workFolder, "Temp");

        updateFolder = Path.Combine(tempFolder, "Update");
        if (!Directory.Exists(updateFolder))
        {
            Directory.CreateDirectory(updateFolder);
        }
        else
        {
            await FileSystemHelper.DeleteFolderContentAsync(updateFolder, cancellationToken).ConfigureAwait(false);
        }

        var zipFilePath = Path.Combine(updateFolder, updateData.UpdateFileName);
        await DownloadHelper.DownloadFileAsync(httpClient, updateData.UpdateDownloadUrl, zipFilePath, downloadProgress, cancellationToken).ConfigureAwait(false);
        if (!File.Exists(zipFilePath))
        {
            throw new InvalidOperationException("Downloaded latest release, but update folder not contains zip file.");
        }

        await UnzipHelper.ExtractZipFileAsync(zipFilePath, updateFolder, cancellationToken).ConfigureAwait(false);

        var appFileName = AppHelper.GetApplicationExecutableFileName();
        var newExeFilePath = Path.Combine(updateFolder, appFileName);
        if (!File.Exists(newExeFilePath))
        {
            throw new InvalidOperationException($"Extracted zip file, but update folder not contains {appFileName} file.");
        }
    }

    public async Task ApplyUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(updateFolder))
        {
            throw new InvalidOperationException("Update folder not exists.");
        }

        var appFileName = AppHelper.GetApplicationExecutableFileName();
        var newExeFilePath = Path.Combine(updateFolder, appFileName);
        if (!File.Exists(newExeFilePath))
        {
            throw new InvalidOperationException($"Update folder not contains {appFileName} file.");
        }

        var newExeVersion = FileSystemHelper.GetExecutableFileVersion(newExeFilePath);
        var installedVersion = GetInstalledVersion();
        if (newExeVersion < installedVersion)
        {
            throw new InvalidOperationException($"{appFileName} in update folder is older than existing {appFileName} in application folder.");
        }

        var exeFilePath = AppHelper.GetApplicationExecutableFilePath();
        var bakFilePath = Path.ChangeExtension(exeFilePath, ".bak");

        File.Move(exeFilePath, bakFilePath, true);
        File.Copy(newExeFilePath, exeFilePath, true);

        await FileSystemHelper.DeleteFolderContentAsync(updateFolder, cancellationToken).ConfigureAwait(false);
    }

    public void RestartApplication(uint delayInSeconds)
    {
        if (delayInSeconds > 10)
        {
            delayInSeconds = 10;
        }

        // To decouple our .exe call from the cmd.exe process, we also need to use "start" here.
        // Since we could have spaces in our .exe path, the path has to be surrounded by quotes.
        // Doing this properly, together with "start", its fist argument has to be empty quotes.
        // See here -> https://stackoverflow.com/questions/2937569/how-to-start-an-application-without-waiting-in-a-batch-file

        var psi = new ProcessStartInfo
        {
            Arguments = $"/C ping 127.0.0.1 -n {delayInSeconds} && start \"\" \"{AppHelper.GetApplicationExecutableFilePath()}\"",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = "cmd.exe"
        };

        var process = Process.Start(psi) ?? throw new InvalidOperationException("The 'Process.Start()' call returned null.");
    }

    public async Task RemoveBakFileIfExistsAsync(CancellationToken cancellationToken = default)
    {
        var exeFilePath = AppHelper.GetApplicationExecutableFilePath();
        var bakFilePath = Path.ChangeExtension(exeFilePath, ".bak");

        if (File.Exists(bakFilePath))
        {
            File.Delete(bakFilePath);
        }
    }

    private static Version GetInstalledVersion()
    {
        string installedExeFile = AppHelper.GetApplicationExecutableFilePath();
        Version installedVersion = FileSystemHelper.GetExecutableFileVersion(installedExeFile);

        return installedVersion;
    }
}
