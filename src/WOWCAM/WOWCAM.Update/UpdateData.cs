namespace WOWCAM.Update;

internal sealed record UpdateData(Version InstalledVersion, Version AvailableVersion, bool UpdateAvailable, string UpdateDownloadUrl, string UpdateFileName);
