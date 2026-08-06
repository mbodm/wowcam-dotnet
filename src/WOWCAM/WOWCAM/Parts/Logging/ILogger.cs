using System.Runtime.CompilerServices;

namespace WOWCAM.Parts.Logging;

internal interface ILogger
{
    string StorageInformation { get; } // Using such a generic term here since this could be a file/database/whatever

    void ClearLog();
    void Log(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    void Log(IEnumerable<string> lines, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    void Log(Exception exception, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    void LogMethodEntry([CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
    void LogMethodExit([CallerMemberName] string caller = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0);
}
