using WOWCAM.Parts.Helper;

namespace WOWCAM.Parts.Config;

internal sealed class ConfigValidatorDefault() : IConfigValidator
{
    public void Validate(ConfigData configData)
    {
        ArgumentNullException.ThrowIfNull(configData);

        // See details and reasons for MaxPathLength value at:
        // https://stackoverflow.com/questions/265769/maximum-filename-length-in-ntfs-windows-xp-and-windows-vista
        // https://stackoverflow.com/questions/23588944/better-to-check-if-length-exceeds-max-path-or-catch-pathtoolongexception

        const int MaxPathLength = 240;

        if (string.IsNullOrWhiteSpace(configData.ApiToken))
        {
            throw new InvalidOperationException("Config does not contain an API token which is required to use the WOWCAM web service.");
        }

        if (string.IsNullOrWhiteSpace(configData.TargetFolder))
        {
            throw new InvalidOperationException("Config does not contain a target folder to download and extract the zip files into.");
        }

        // Easy to foresee the max length here of some fixed folder, like temp folder, or anything like that. But not that easy to foresee the max length of
        // target folder, when considering content of zip file (files and subfolders). Therefore just using half of MAX_PATH here, as some "rule of thumb".
        // If in a rare case a full dest path, coming from zip content, exceeds MAX_PATH, it seems OK to let the unzip operation fail gracefully on its own.

        ValidateFolder(configData.TargetFolder, "target", MaxPathLength / 2);

        if (!configData.AddonUrls.Any())
        {
            throw new InvalidOperationException("Config does not contain any addon URL entries and so there is nothing to download.");
        }

        if (configData.AddonUrls.Any(url => !CurseHelper.IsAddonPageUrl(url)))
        {
            throw new InvalidOperationException("Config contains at least one addon URL entry which is not a valid Curse addon URL.");
        }
    }

    private static void ValidateFolder(string folderValue, string folderName, int maxChars)
    {
        if (folderValue.Length > maxChars)
        {
            throw new InvalidOperationException($"Config contains a {folderName} folder path which is too long (make sure the given path is shorter than {maxChars} characters).");
        }

        // I decided to NOT create any configured folder by code since the default config makes assumptions here (like WoW's location in %PROGRAMFILES(X86)% folder)

        if (!Directory.Exists(folderValue))
        {
            throw new InvalidOperationException($"Config contains a {folderName} folder which does not exist (make sure the given path is a valid absolute path to an existing folder).");
        }
    }
}
