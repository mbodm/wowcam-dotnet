using WOWCAM;
using WOWCAM.Parts.Helper;

var program = AppHelper.GetApplicationExecutableFileName().ToLower();
var version = AppHelper.GetApplicationVersion();

Console.WriteLine();
Console.WriteLine($"{program} {version} (by MBODM 08/2026)");
Console.WriteLine();

var needsNewLineOnError = false;
var countOfAddons = 0;
try
{
    using var httpClient = new HttpClient();
    var wowcam = new Wowcam(httpClient);

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += async (_, e) =>
    {
        e.Cancel = true;
        await cts.CancelAsync().ConfigureAwait(false);
    };

    var result = await wowcam.RunAsync(new Progress<IEnumerable<string>>(addonNames =>
    {
        Console.Write($"Processing {addonNames.Count()} addons ...");
        countOfAddons = addonNames.Count();
        needsNewLineOnError = true;
    }),
    new Progress<byte>(percent =>
    {
        if (percent % 4 == 0)
        {
            Console.Write('.');
        }
    }),
    cts.Token).ConfigureAwait(false);

    var duration = $"{Convert.ToDouble(result.DurationInMilliseconds) / 1000:F2}";
    var updated = $"{result.UpdatedAddons}/{countOfAddons}";
    Console.WriteLine($" Finished after {duration} seconds ({updated} addons updated)");
    Console.WriteLine();

    Console.WriteLine("Have a nice day.");
    if (OperatingSystem.IsMacOS()) Console.WriteLine();
    Environment.Exit(0);
}
catch (Exception ex)
{
    if (needsNewLineOnError)
    {
        Console.WriteLine();
        Console.WriteLine();
    }

    if (ex is OperationCanceledException)
    {
        Console.WriteLine($"Info: {ex.Message}");
        Console.WriteLine();
        Console.WriteLine("Have a nice day.");
        if (OperatingSystem.IsMacOS()) Console.WriteLine();
        Environment.Exit(255);
    }
    else
    {
        Console.WriteLine($"Error: {ex.Message}");
        if (OperatingSystem.IsMacOS()) Console.WriteLine();
        Environment.Exit(1);
    }
}
