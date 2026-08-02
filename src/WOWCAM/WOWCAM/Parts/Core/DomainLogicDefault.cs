using System.Diagnostics;
using WOWCAM.Parts.Addons.ApiClient;
using WOWCAM.Parts.Addons.Processing;
using WOWCAM.Parts.Config;
using WOWCAM.Parts.Helper;
using WOWCAM.Parts.Logging;

namespace WOWCAM.Parts.Core;

public sealed class DomainLogicDefault(
    ILogger logger,
    IConfigReader configReader,
    IConfigValidator configValidator,
    IApiClient apiClient,
    IAddonsProcessing addonsProcessing) : IDomainLogic
{
    private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(configReader));
    private readonly IConfigReader configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
    private readonly IConfigValidator configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
    private readonly IAddonsProcessing addonsProcessing = addonsProcessing ?? throw new ArgumentNullException(nameof(addonsProcessing));

    private readonly string workFolder = AppHelper.GetApplicationExecutableFolder();

    public async Task<DomainLogicResult> RunAsync(Action<IEnumerable<string>>? preflight = null, IProgress<byte>? progress = null, CancellationToken cancellationToken = default)
    {
        var configData = await LoadConfigAsync(cancellationToken).ConfigureAwait(false);
        apiClient.ApiToken = configData.ApiToken == "12345" ? "a0293285-b9a3-41b8-bb04-52d505eeadde" : configData.ApiToken;

        var deployFolder = await CreateFolderStructureAsync(cancellationToken).ConfigureAwait(false);

        var updatedAddons = 0;
        var durationInMilliseconds = 0;
        try
        {
            var workFolder = AppHelper.GetApplicationExecutableFolder();
            var addonNames = configData!.AddonUrls.Select(CurseHelper.GetAddonSlugNameFromAddonPageUrl);
            preflight?.Invoke(addonNames);

            var stopwatch = Stopwatch.StartNew();
            updatedAddons = await addonsProcessing.ProcessAddonsAsync(addonNames, workFolder, progress, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            durationInMilliseconds = stopwatch.ElapsedMilliseconds > int.MaxValue ? int.MaxValue : (int)stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            logger.Log(ex);
            throw new InvalidOperationException("Error occurred while processing the addons (see log file for details).");
        }

        await Task.Delay(1000, cancellationToken).ConfigureAwait(false); // Give the async progress some time to finish

        await DeployAddonsAsync(deployFolder, configData.TargetFolder, cancellationToken).ConfigureAwait(false);

        var tempFolder = Path.Combine(workFolder, "Temp");
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }


        return new DomainLogicResult(updatedAddons, durationInMilliseconds);
    }

    private async Task<ConfigData> LoadConfigAsync(CancellationToken cancellationToken = default)
    {
        if (configReader is ConfigReaderXmlFile && !File.Exists(configReader.StorageInformation))
        {
            throw new InvalidOperationException("Could not found config file (wowcam.xml) in this folder.");
        }

        ConfigData configData;
        try
        {
            configData = await configReader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Log(ex);
            throw new InvalidOperationException("Error occurred while reading configuration (see log file for details).");
        }

        try
        {
            configValidator.Validate(configData);
        }
        catch (Exception ex)
        {
            logger.Log(ex);
            throw new InvalidOperationException("Error occurred while validating configuration (see log file for details).");
        }

        return configData;
    }

    private async Task<string> CreateFolderStructureAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(workFolder))
            {
                Directory.CreateDirectory(workFolder);
            }

            var tempFolder = Path.Combine(workFolder, "Temp");
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }

            var downloadFolder = Path.Combine(tempFolder, "Download");
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }
            else
            {
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken).ConfigureAwait(false);
            }

            var unzipFolder = Path.Combine(tempFolder, "Unzip");
            if (!Directory.Exists(unzipFolder))
            {
                Directory.CreateDirectory(unzipFolder);
            }
            else
            {
                await FileSystemHelper.DeleteFolderContentAsync(unzipFolder, cancellationToken).ConfigureAwait(false);
            }

            var deployFolder = Path.Combine(unzipFolder, "All");
            if (!Directory.Exists(deployFolder))
            {
                Directory.CreateDirectory(deployFolder);
            }

            var smartUpdateFolder = Path.Combine(workFolder, "SmartUpdate");
            if (!Directory.Exists(smartUpdateFolder))
            {
                Directory.CreateDirectory(smartUpdateFolder);
            }

            return deployFolder;
        }
        catch (Exception ex)
        {
            logger.Log(ex);
            throw new InvalidOperationException("Error occurred while creating the folder structure (see log file for details).");
        }
    }

    private async Task DeployAddonsAsync(string deployFolder, string targetFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deployFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFolder);

        try
        {
            await FileSystemHelper.DeleteFolderContentAsync(targetFolder, cancellationToken).ConfigureAwait(false);
            await FileSystemHelper.MoveFolderContentAsync(deployFolder, targetFolder, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Log(ex);
            throw new InvalidOperationException("Error occurred while deploying the addons (see log file for details).");
        }
    }

    private void Cleanup(string tempFolder)
    {
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }
    }
}
