namespace WOWCAM.Parts.Helper;

internal sealed class CurseHelper
{
    public static bool IsAddonPageUrl(string url)
    {
        // Example -> https://www.curseforge.com/wow/addons/deadly-boss-mods
        url = GuardAndNormalize(url);
        return url.StartsWith("https://www.curseforge.com/wow/addons/") && !url.EndsWith("/addons");
    }

    public static bool IsAddonDownloadUrl(string url)
    {
        // Example -> https://mediafilez.forgecdn.net/files/4485/146/DBM-10.0.35.zip
        url = GuardAndNormalize(url);
        return url.StartsWith("https://mediafilez.forgecdn.net/files/") && url.EndsWith(".zip");
    }

    public static string GetAddonSlugNameFromAddonPageUrl(string url)
    {
        // Example -> https://www.curseforge.com/wow/addons/deadly-boss-mods
        url = GuardAndNormalize(url);
        return IsAddonPageUrl(url) ? url.Split("https://www.curseforge.com/wow/addons/").Last().ToLower() : string.Empty;
    }

    public static string GetZipFileNameFromAddonDownloadUrl(string url)
    {
        // Example -> https://mediafilez.forgecdn.net/files/4485/146/DBM-10.0.35.zip
        url = GuardAndNormalize(url);
        return IsAddonDownloadUrl(url) ? Path.GetFileName(new Uri(url).LocalPath) : string.Empty;
    }

    private static string GuardAndNormalize(string url) =>
        url?.Trim().TrimEnd('/') ?? string.Empty;
}
