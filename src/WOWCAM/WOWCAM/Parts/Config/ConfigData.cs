namespace WOWCAM.Parts.Config;

public sealed record ConfigData(string ApiToken, string TargetFolder, IEnumerable<string> AddonUrls)
{
    public static ConfigData Empty() => new(string.Empty, string.Empty, []);
}
