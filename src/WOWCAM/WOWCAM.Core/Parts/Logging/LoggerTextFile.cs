using System.Runtime.CompilerServices;
using System.Text;
using WOWCAM.Core.Parts.Extensions;

namespace WOWCAM.Core.Parts.Logging;

internal sealed class LoggerTextFile : ILogger
{
    private readonly object syncRoot = new();
    private readonly string newLine = Environment.NewLine;

    private readonly string logFile;

    public LoggerTextFile(string logFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logFile);

        this.logFile = logFile;
    }

    public string StorageInformation => logFile;

    public void ClearLog()
    {
        lock (syncRoot)
        {
            File.WriteAllText(StorageInformation, string.Empty);
        }
    }

    public void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException($"'{nameof(message)}' cannot be null or whitespace.", nameof(message));
        }

        lock (syncRoot)
        {
            AppendLogEntry("Message", file, line, message);
        }
    }

    public void Log(IEnumerable<string> lines, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (!lines.Any())
        {
            throw new ArgumentNullException(nameof(lines), "Enumerable is empty.");
        }

        var message = string.Join(newLine, lines);

        lock (syncRoot)
        {
            AppendLogEntry("Message", file, line, message);
        }
    }

    public void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = $"Exception occurred{newLine}";

        message += $" => Exception-Type       = {exception.GetType().Name}{newLine}";
        message += $" => Exception-Message    = {exception.Message}";

        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            var formattedStackTrace = exception.StackTrace.Replace(newLine, string.Empty).Replace("   at ", $"{newLine}     at ");

            message += $"{newLine} => Exception-StackTrace = {formattedStackTrace}";
        }

        lock (syncRoot)
        {
            AppendLogEntry("Exception", file, line, message);
        }
    }

    public void LogMethodEntry([CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        var message = $"Reached entry of {caller}() method";

        lock (syncRoot)
        {
            AppendLogEntry("Message", file, line, message);
        }
    }

    public void LogMethodExit([CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        var message = $"Reached exit of {caller}() method";

        lock (syncRoot)
        {
            AppendLogEntry("Message", file, line, message);
        }
    }

    private void AppendLogEntry(string header, string file, int line, string message)
    {
        var now = DateTime.Now.ToIso8601(true);

        file = Path.GetFileName(file);

        var text = $"[{now}] {header}{newLine}File: {file}{newLine}Line: {line}{newLine}{message}{newLine}{newLine}";

        File.AppendAllText(StorageInformation, text, Encoding.UTF8);
    }
}
