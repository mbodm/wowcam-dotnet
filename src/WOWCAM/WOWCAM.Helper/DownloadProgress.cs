namespace WOWCAM.Helper;

public sealed record DownloadProgress(string Url, bool PreTransfer, long ReceivedBytes, long TotalBytes, bool TransferFinished);
