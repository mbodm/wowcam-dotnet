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
    var exeFolder = AppHelper.GetApplicationExecutableFolder();

    var logFile = Path.Combine(exeFolder, "WOWCAM.log");
    var logger = new LoggerTextFile(logFile);
    var xmlFile = Path.Combine(exeFolder, "wowcam.xml");
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

    var configData = await domainLogic.LoadConfigAsync(cancellationToken);
    apiClient.ApiToken = configData.ApiToken == "12345" ? "a0293285-b9a3-41b8-bb04-52d505eeadde" : configData.ApiToken;

    var workFolder = AppHelper.GetApplicationExecutableFolder();
    var deployFolder = await domainLogic.InitAsync(workFolder, cancellationToken).ConfigureAwait(false);

    var sw = Stopwatch.StartNew();

    var updatedAddons = await domainLogic.ProcessAddonsAsync(addonNames =>
    {
        Console.Write($"Processing {addonNames.Count()} addons...");
    },
    new Progress<byte>(percent => 
    {
        if (percent % 2 == 0) Console.Write('.');

    }), cts.Token).ConfigureAwait(false);

    await domainLogic.DeployAddonsAsync(deployFolder, configData.TargetFolder, cancellationToken).ConfigureAwait(false);

    sw.Stop();

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
