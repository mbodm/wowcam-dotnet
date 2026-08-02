using WOWCAM.Parts.Core;
using WOWCAM.Parts.Helper;

namespace WOWCAM.Parts.Addons.Curse;

public sealed class ServerFetchDefault(IHttpClientProvider httpClientProvider) : IServerFetch
{
    private readonly IHttpClientProvider httpClientProvider = httpClientProvider ?? throw new ArgumentNullException(nameof(httpClientProvider));

    public async Task DownloadAddonAsync(string downloadUrl, string zipFilePath, IProgress<byte>? downloadProgress = default, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFilePath);
        ArgumentNullException.ThrowIfNull(downloadProgress);

        var httpClient = httpClientProvider.GetHttpClient();

        await DownloadHelper.DownloadFileAsync(httpClient, downloadUrl, zipFilePath, new Progress<DownloadProgress>(p =>
        {
            var downloadPercent = CalcDownloadPercent(p.ReceivedBytes, p.TotalBytes);

            downloadProgress?.Report(downloadPercent);
        }),
        cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> UnzipAddonAsync(string zipFilePath, string unzipFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(unzipFolder);

        var zipFileName = Path.GetFileName(zipFilePath);
        if (!await UnzipHelper.ValidateZipFileAsync(zipFilePath, cancellationToken))
        {
            throw new InvalidOperationException($"It seems the addon ZIP file ('{zipFileName}') is corrupted, cause ZIP file validation failed.");
        }

        var unzipFolderName = Path.GetFileNameWithoutExtension(zipFileName);
        var unzipFolderPath = Path.Combine(unzipFolder, unzipFolderName);

        if (!Directory.Exists(unzipFolderPath))
        {
            Directory.CreateDirectory(unzipFolderPath);
        }

        await UnzipHelper.ExtractZipFileAsync(zipFilePath, unzipFolderPath, cancellationToken).ConfigureAwait(false);

        return unzipFolderPath;
    }

    private static byte CalcDownloadPercent(long bytesReceived, long bytesTotal)
    {
        // Doing casts inside try/catch block (just to be sure)

        try
        {
            var exact = (double)bytesReceived / bytesTotal;
            var exactPercent = exact * 100;
            var roundedPercent = (byte)Math.Round(exactPercent);
            var cappedPercent = roundedPercent > 100 ? (byte)100 : roundedPercent; // Cap it (just to be sure)

            return cappedPercent;
        }
        catch
        {
            return 0;
        }
    }
}
