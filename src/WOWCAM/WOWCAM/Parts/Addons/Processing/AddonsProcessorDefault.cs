using WOWCAM.Parts.Addons.ApiClient;
using WOWCAM.Parts.Addons.Curse;
using WOWCAM.Parts.Addons.SmartUpdate;
using WOWCAM.Parts.Helper;

namespace WOWCAM.Parts.Addons.Processing;

public sealed class AddonsProcessorDefault(IApiClient apiClient, IServerFetch serverFetch, ISmartUpdate smartUpdate) : IAddonsProcessor
{
    private readonly IApiClient apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    private readonly IServerFetch serverFetch = serverFetch ?? throw new ArgumentNullException(nameof(serverFetch));
    private readonly ISmartUpdate smartUpdate = smartUpdate ?? throw new ArgumentNullException(nameof(smartUpdate));

    public async Task<int> ProcessAddonsAsync(IEnumerable<string> addonNames, string workFolder, string targetFolder,
        IProgress<byte>? progress = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addonNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(workFolder);

        if (!addonNames.Any())
        {
            return 0;
        }

        // Folders

        var tempFolder = Path.Combine(workFolder, "Temp");
        if (!Directory.Exists(tempFolder))
        {
            Directory.CreateDirectory(tempFolder);
        }
        else
        {
            await FileSystemHelper.DeleteFolderContentAsync(tempFolder, cancellationToken).ConfigureAwait(false);
        }

        var downloadFolder = Path.Combine(tempFolder, "Download");
        Directory.CreateDirectory(downloadFolder);

        var unzipFolder = Path.Combine(tempFolder, "Unzip");
        Directory.CreateDirectory(unzipFolder);

        var deployFolder = Path.Combine(unzipFolder, "All");
        Directory.CreateDirectory(deployFolder);

        var addons = await apiClient.GetAddonDownloadUrlsAsync(addonNames, cancellationToken).ConfigureAwait(false);
        if (!addons.Any())
        {
            return 0;
        }

        var progressHelper = new ProgressHelper(addons.Count(), progress);

        //await smartUpdate.LoadAsync(cancellationToken).ConfigureAwait(false);

        // Concurrently do for every addon -> "Use SmartUpdate" OR "Download & Unzip"

        var updatedAddonsCounter = 0;

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
                // CurseFetch

                var zipFileName = CurseHelper.GetZipFileNameFromAddonDownloadUrl(downloadUrl);
                var zipFilePath = Path.Combine(downloadFolder, zipFileName);

                var downloadProgress = new Progress<byte>(b => progressHelper.ReportDownloadProgress(index, b));
                await serverFetch.DownloadAddonAsync(downloadUrl, zipFilePath, downloadProgress, cancellationToken).ConfigureAwait(false);
                progressHelper.ReportDownloadFinished(index);

                var zipContentFolder = await serverFetch.UnzipAddonAsync(zipFilePath, unzipFolder, cancellationToken).ConfigureAwait(false);
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

        // Give the last addon's async progress (for i.e. UI updates) some time to finish
        // Give the last addon's file I/O (cause of i.e. virus scanner) some time to finish
        await Task.Delay(1500, cancellationToken).ConfigureAwait(false);

        await FileSystemHelper.DeleteFolderContentAsync(targetFolder, cancellationToken).ConfigureAwait(false);
        await FileSystemHelper.MoveFolderContentAsync(deployFolder, targetFolder, cancellationToken).ConfigureAwait(false);

        return updatedAddonsCounter;
    }
}
