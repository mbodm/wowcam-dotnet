using System.Diagnostics;
using WOWCAM.Parts.Addons.ApiClient;
using WOWCAM.Parts.Addons.Core;
using WOWCAM.Parts.Config;
using WOWCAM.Parts.Helper;
using WOWCAM.Parts.Logging;

namespace WOWCAM.Parts.Domain;

internal sealed class DomainLogicDefault : IDomainLogic
{
    private readonly string workFolder;
    private readonly ILogger logger;
    private readonly IConfigReader configReader;
    private readonly IConfigValidator configValidator;
    private readonly IApiClient apiClient;
    private readonly IAddonsProcessor addonsProcessor;

    public DomainLogicDefault(string workFolder, ILogger logger, IConfigReader configReader, IConfigValidator configValidator, IApiClient apiClient, IAddonsProcessor addonsProcessor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workFolder);
        this.workFolder = workFolder;

        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.configReader = configReader ?? throw new ArgumentNullException(nameof(configReader));
        this.configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
        this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        this.addonsProcessor = addonsProcessor ?? throw new ArgumentNullException(nameof(addonsProcessor));
    }

    public async Task<DomainLogicResult> RunAsync(IProgress<IEnumerable<string>>? preflight = default, IProgress<byte>? progress = default, CancellationToken cancellationToken = default)
    {
        try
        {
            ConfigData configData;
            try
            {
                configData = await configReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                throw new InvalidOperationException("Error occurred while reading configuration (see log file for details).", ex);
            }

            try
            {
                configValidator.Validate(configData);
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                throw new InvalidOperationException("Error occurred while validating configuration (see log file for details).", ex);
            }

            apiClient.ApiToken = configData.ApiToken == "12345" ? "a0293285-b9a3-41b8-bb04-52d505eeadde" : configData.ApiToken;

            try
            {
                if (!Directory.Exists(workFolder))
                {
                    Directory.CreateDirectory(workFolder);
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                throw new InvalidOperationException("Error occurred while creating the folder structure (see log file for details).", ex);
            }

            int countOfUpdatedAddons;
            int durationInMilliseconds;
            try
            {
                var addonNames = configData!.AddonUrls.Select(CurseHelper.GetAddonSlugNameFromAddonPageUrl);
                preflight?.Report(addonNames);

                var stopwatch = Stopwatch.StartNew();
                countOfUpdatedAddons = await addonsProcessor.ProcessAddonsAsync(addonNames, workFolder, configData.TargetFolder, progress, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                durationInMilliseconds = stopwatch.ElapsedMilliseconds > int.MaxValue ? int.MaxValue : (int)stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                logger.Log(ex);
                throw new InvalidOperationException("Error occurred while processing the addons (see log file for details).", ex);
            }

            //if (Directory.Exists(tempFolder))
            //{
            //    Directory.Delete(tempFolder, true);
            //}

            return new DomainLogicResult(countOfUpdatedAddons, durationInMilliseconds);

        }
        catch (Exception ex)
        {
            if (ex.InnerException is OperationCanceledException || ex.InnerException is TaskCanceledException)
            {
                throw new OperationCanceledException("The program was cancelled.", ex);
            }

            throw;
        }
    }
}
