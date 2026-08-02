using WOWCAM.Parts.Addons.Processing;
using WOWCAM.Parts.Config;
using WOWCAM.Parts.Helper;
using WOWCAM.Parts.Logging;

namespace WOWCAM.Parts.Core;

public sealed class DomainLogicDefault(ILogger logger, IConfigReader configReader, IConfigValidator configValidator, IAddonsProcessing addonsProcessing) : IDomainLogic
{
    private readonly IConfigReader configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
    private readonly IConfigValidator configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
    private readonly IAddonsProcessing addonsProcessing = addonsProcessing ?? throw new ArgumentNullException(nameof(addonsProcessing));

    private ConfigData? configData = null;
    private string workFolder = string.Empty;

    public async Task<ConfigData> LoadConfigAsync(CancellationToken cancellationToken = default)
    {
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

    public HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient();

        // The HttpClient shall look a bit more like a Chrome Browser

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        httpClient.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br, zstd");
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "\"Chromium\";v=\"150\", \"Not:A-Brand\";v=\"99\", \"Google Chrome\";v=\"150\"");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
        httpClient.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");

        return httpClient;
    }

    public async Task<string> InitAsync(string workFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workFolder);

        try
        {
            this.workFolder = workFolder;
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
                await FileSystemHelper.DeleteFolderContentAsync(downloadFolder, cancellationToken).ConfigureAwait(false);
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

    public async Task DeployAddonsAsync(string deployFolder, string targetFolder, CancellationToken cancellationToken = default)
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

    public void CleanUp(string tempFolder)
    {
        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }
    }

    public async Task<uint> ProcessAddonsAsync(Action<IEnumerable<string>>? preProgress = null, IProgress<byte>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var workFolder = AppHelper.GetApplicationExecutableFolder();
            var addonNames = configData!.AddonUrls.Select(CurseHelper.GetAddonSlugNameFromAddonPageUrl);
            preProgress?.Invoke(addonNames);
            var updatedAddons = await addonsProcessing.ProcessAddonsAsync(addonNames, workFolder, progress, cancellationToken).ConfigureAwait(false);
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false); // Give async progress time to finish
            return updatedAddons;
        }
        catch (Exception ex)
        {
            logger.Log(ex);
            throw new InvalidOperationException("Error occurred while processing addons (see log file for details).");
        }
    }
}
