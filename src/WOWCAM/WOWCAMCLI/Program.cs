using System.Diagnostics;
using WOWCAM.Parts.Addons.ApiClient;
using WOWCAM.Parts.Addons.Processing;
using WOWCAM.Parts.Addons.SmartUpdate;
using WOWCAM.Parts.Config;
using WOWCAM.Parts.Core;
using WOWCAM.Parts.Helper;
using WOWCAM.Parts.Logging;

var title = $"{AppHelper.GetApplicationExecutableFileName().ToLower()} {AppHelper.GetApplicationVersion()} (by MBODM 08/2026)";
Console.WriteLine();
Console.WriteLine(title);
Console.WriteLine();

try
{
    var workFolder = AppHelper.GetApplicationExecutableFolder();

    var logFile = Path.Combine(workFolder, "WOWCAM.log");
    var logger = new LoggerTextFile(logFile);
    var xmlFile = Path.Combine(workFolder, "wowcam.xml");
    var configReader = new ConfigReaderXmlFile(xmlFile);
    var configValidator = new ConfigValidatorDefault();
    using var httpClient = new HttpClient();
    var httpClientProvider = new HttpClientProviderDefault(httpClient);
    var apiClient = new ApiClientDefault(httpClientProvider);
    var smartUpdate = new SmartUpdateDefault();
    var addonsProcessing = new AddonsProcessingDefault(apiClient, smartUpdate, httpClientProvider);
    var domainLogic = new DomainLogicDefault(logger, configReader, configValidator, addonsProcessing);


    var cts = new CancellationTokenSource();
    var cancellationToken = cts.Token;

    var sw = Stopwatch.StartNew();
    var updatedAddons = await domainLogic.RunAsync(addonNames =>
    {
        Console.Write($"Processing {addonNames.Count()} addons ...");
    },
    new Progress<byte>(percent =>
    {
        if (percent % 2 == 0)
        {
            Console.Write('.');
        }
    }),
    cts.Token).ConfigureAwait(false);
    sw.Stop();

    await domainLogic.DeployAddonsAsync(deployFolder, configData.TargetFolder, cancellationToken).ConfigureAwait(false);

    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine($"Finished after {Convert.ToDouble(sw.ElapsedMilliseconds) / 1000} seconds ({updatedAddons} addons updated)");
    Console.WriteLine();

    //await smartUpdateFeature.SaveAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("Have a nice day.");
Environment.Exit(0);
