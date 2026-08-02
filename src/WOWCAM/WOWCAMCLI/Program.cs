using WOWCAM.Parts.Addons.ApiClient;
using WOWCAM.Parts.Addons.Processing;
using WOWCAM.Parts.Addons.SmartUpdate;
using WOWCAM.Parts.Config;
using WOWCAM.Parts.Core;
using WOWCAM.Parts.Helper;
using WOWCAM.Parts.Logging;

var program = AppHelper.GetApplicationExecutableFileName().ToLower();
var version = AppHelper.GetApplicationVersion();

Console.WriteLine();
Console.WriteLine($"{program} {version} (by MBODM 08/2026)");
Console.WriteLine();

try
{
    var workFolder = AppHelper.GetApplicationExecutableFolder();

    var logger = new LoggerTextFile(Path.Combine(workFolder, "wowcam.log"));
    var configReader = new ConfigReaderXmlFile(Path.Combine(workFolder, "wowcam.xml"));
    var configValidator = new ConfigValidatorDefault();
    using var httpClient = new HttpClient();
    var httpClientProvider = new HttpClientProviderDefault(httpClient);
    var apiClient = new ApiClientDefault(httpClientProvider);
    var smartUpdate = new SmartUpdateDefault();
    var addonsProcessing = new AddonsProcessingDefault(apiClient, smartUpdate, httpClientProvider);
    var domainLogic = new DomainLogicDefault(logger, configReader, configValidator, apiClient, addonsProcessing);


    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += async (sender, e) =>
    {
        e.Cancel = true;

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("Info: Program was cancelled by user.");
        Console.WriteLine();
        Console.WriteLine("Cancelling all tasks now....");

        cts.Cancel();
    };

    var result = await domainLogic.RunAsync(addonNames =>
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

    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine($"Finished after {Convert.ToDouble(result.DurationInMilliseconds) / 1000:F2} seconds ({result.UpdatedAddons} addons updated)");
    Console.WriteLine();

    Console.WriteLine("Have a nice day.");
    Environment.Exit(0);

    //await smartUpdateFeature.SaveAsync().ConfigureAwait(false);
}

catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.WriteLine("Cancelled.");
    Console.WriteLine();
    Console.WriteLine("Have a nice day.");
    Environment.Exit(255);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Environment.Exit(1);
}
