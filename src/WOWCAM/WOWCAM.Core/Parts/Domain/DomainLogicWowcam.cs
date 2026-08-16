using WOWCAM.Core.Parts.Abstractions;
using WOWCAM.Core.Parts.Addons.ApiClient;
using WOWCAM.Core.Parts.Addons.Curse;
using WOWCAM.Core.Parts.Addons.Processing;
using WOWCAM.Core.Parts.Addons.SmartUpdate;
using WOWCAM.Core.Parts.Config;
using WOWCAM.Core.Parts.Logging;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Domain;

public sealed class DomainLogicWowcam : IDomainLogic
{
    // This class acts as the poor man's DI composition root (injecting the default implementations of the modules)
    // and exposes the domain logic by using the facade pattern (acting as a facade for the abstract domain logic)

    private readonly DomainLogicDefault domainLogic;

    public DomainLogicWowcam(HttpClient httpClient)
    {
        // Compose dependencies

        var workFolder = AppHelper.GetApplicationExecutableFolder();
        var logger = new LoggerTextFile(Path.Combine(workFolder, "wowcam.log"));
        var configReader = new ConfigReaderXmlFile(Path.Combine(workFolder, "wowcam.xml"));
        var configValidator = new ConfigValidatorDefault();
        var httpClientProvider = new HttpClientProviderDefault(httpClient);
        var apiClient = new ApiClientDefault(httpClientProvider);
        var serverFetch = new ServerFetchDefault(httpClientProvider);
        var smartUpdate = new SmartUpdateDefault(workFolder);
        var addonsProcessing = new AddonsProcessorDefault(apiClient, serverFetch, smartUpdate);

        domainLogic = new DomainLogicDefault(workFolder, logger, configReader, configValidator, apiClient, addonsProcessing);
    }

    public Task<DomainLogicResult> RunAsync(IProgress<IEnumerable<string>>? preflight = default, IProgress<byte>? progress = default, CancellationToken cancellationToken = default)
    {
        return domainLogic.RunAsync(preflight, progress, cancellationToken);
    }
}
