using WOWCAM.Parts.Addons.ApiClient;
using WOWCAM.Parts.Addons.Curse;
using WOWCAM.Parts.Addons.Processing;
using WOWCAM.Parts.Addons.SmartUpdate;
using WOWCAM.Parts.Config;
using WOWCAM.Parts.Core;
using WOWCAM.Parts.Helper;
using WOWCAM.Parts.Logging;

namespace WOWCAM
{
    public sealed class Wowcam : IDomainLogic
    {
        private readonly DomainLogicDefault domainLogic;

        public Wowcam(HttpClient httpClient)
        {
            // Compose dependencies

            var workFolder = AppHelper.GetApplicationExecutableFolder();
            var logger = new LoggerTextFile(Path.Combine(workFolder, "wowcam.log"));
            var configReader = new ConfigReaderXmlFile(Path.Combine(workFolder, "wowcam.xml"));
            var configValidator = new ConfigValidatorDefault();
            var httpClientProvider = new HttpClientProviderDefault(httpClient);
            var apiClient = new ApiClientDefault(httpClientProvider);
            var serverFetch = new ServerFetchDefault(httpClientProvider);
            var smartUpdate = new SmartUpdateDefault();
            var addonsProcessing = new AddonsProcessorDefault(apiClient, serverFetch, smartUpdate);

            domainLogic = new DomainLogicDefault(workFolder, logger, configReader, configValidator, apiClient, addonsProcessing);
        }

        public async Task<DomainLogicResult> RunAsync(Action<IEnumerable<string>>? preflight = default, IProgress<byte>? progress = default, CancellationToken cancellationToken = default)
        {
            //if (!File.Exists(configReader.StorageInformation))
            //{
            //    throw new InvalidOperationException("Could not found config file (wowcam.xml) in this folder.");
            //}

            try
            {
                return await domainLogic.RunAsync(preflight, progress, cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is OperationCanceledException || ex.InnerException is TaskCanceledException)
                {
                    throw new OperationCanceledException("Program was cancelled by user.", ex);
                }

                throw;
            }
        }
    }
}
