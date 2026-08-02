using WOWCAM.Parts.Addons.ApiClient;
using WOWCAM.Parts.Addons.SmartUpdate;
using WOWCAM.Parts.Core;
using WOWCAM.Parts.Helper;

namespace WOWCAM.Parts.Addons.Processing;

public sealed class AddonsProcessingDefault(IApiClient apiClient, ISmartUpdate smartUpdate, IHttpClientProvider httpClientProvider) : IAddonsProcessing
{
    private readonly IApiClient apiClient = apiClient;
    private readonly ISmartUpdate smartUpdate = smartUpdate;
    private readonly IHttpClientProvider httpClientProvider = httpClientProvider;

    public async Task<uint> ProcessAddonsAsync(IEnumerable<string> addonNames, string workFolder, IProgress<byte>? progress = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addonNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(workFolder);

        if (!addonNames.Any())
        {
            return 0;
        }

        var downloadFolder = Path.Combine(workFolder, "Temp", "Download");
        if (!Directory.Exists(downloadFolder))
        {
            Directory.CreateDirectory(downloadFolder);
        }

        var unzipFolder = Path.Combine(workFolder, "Temp", "Unzip");
        if (!Directory.Exists(unzipFolder))
        {
            Directory.CreateDirectory(unzipFolder);
        }

        var deployFolder = Path.Combine(unzipFolder, "All");
        if (!Directory.Exists(deployFolder))
        {
            Directory.CreateDirectory(deployFolder);
        }

        var addons = await apiClient.GetAddonDownloadUrlsAsync(addonNames, cancellationToken).ConfigureAwait(false);
        if (!addons.Any())
        {
            return 0;
        }

        var progressHelper = new ProgressHelper(addons.Count(), progress);

        //await smartUpdate.LoadAsync(cancellationToken).ConfigureAwait(false);

        // Concurrently do for every addon -> "Use SmartUpdate" OR "Download & Unzip"

        uint updatedAddonsCounter = 0;

        var tasks = addons.Select(async (addon, index) =>
        {
            var addonName = addon.AddonSlug;
            var downloadUrl = addon.DownloadUrl;

            if (smartUpdate.AddonExists(addonName, downloadUrl))
            {
                // SmartUpdate

                await smartUpdate.DeployAddonAsync(addonName, deployFolder).ConfigureAwait(false);

                progressHelper.ReportUnzipFinished(index);
            }
            else
            {
                var zipFileName = CurseHelper.GetZipFileNameFromAddonDownloadUrl(downloadUrl);
                var zipFilePath = Path.Combine(downloadFolder, zipFileName);

                await DownloadAddonAsync(addonName, downloadUrl, zipFilePath, b => progressHelper.ReportDownloadProgress(index, b), cancellationToken).ConfigureAwait(false);
                progressHelper.ReportDownloadFinished(index);

                var zipContentFolder = await UnzipAddonAsync(zipFilePath, unzipFolder, cancellationToken).ConfigureAwait(false);
                progressHelper.ReportUnzipFinished(index);

                //smartUpdate.AddOrUpdateAddon(addonName, downloadUrl);

                await FileSystemHelper.CopyFolderContentAsync(zipContentFolder, deployFolder, cancellationToken).ConfigureAwait(false);

                Interlocked.Increment(ref updatedAddonsCounter);
            }
        });

        // Concurrently handle addons
        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Sequentially handle addons (outcommented - just for performance comparisson)
        //foreach (var task in tasks)
        //{
        //    await task.ConfigureAwait(false);
        //}

        // await smartUpdate.SaveAsync(cancellationToken).ConfigureAwait(false);

        foreach (var folder in Directory.EnumerateDirectories(deployFolder))
        {
        }


        return updatedAddonsCounter;
    }

    private async Task DownloadAddonAsync(string addonName, string downloadUrl, string zipFilePath, Action<byte> downloadProgress, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientProvider.GetHttpClient();

        await DownloadHelper.DownloadFileAsync(httpClient, downloadUrl, zipFilePath, new Progress<DownloadProgress>(p =>
        {
            var downloadPercent = CalcDownloadPercent(p.ReceivedBytes, p.TotalBytes);
            downloadProgress(downloadPercent);
        }),
        cancellationToken).ConfigureAwait(false);
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

    private static async Task<string> UnzipAddonAsync(string zipFilePath, string unzipFolder, CancellationToken cancellationToken = default)
    {
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
}
