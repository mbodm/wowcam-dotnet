namespace WOWCAM.Parts.Helper;

internal sealed record DownloadProgress(string Url, bool PreTransfer, long ReceivedBytes, long TotalBytes, bool TransferFinished);
