using System.Xml.Linq;

namespace WOWCAM.Parts.Config;

public sealed class ConfigReaderXmlFile(string xmlFile) : IConfigReader
{
    private readonly string xmlFile = xmlFile;

    public async Task<ConfigData> ReadAsync(CancellationToken cancellationToken = default)
    {
        using var fileStream = new FileStream(xmlFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        CheckBasicFileStructure(doc);

        var activeProfile = doc.Root?.Element("general")?.Element("profile")?.Value?.Trim() ?? throw new InvalidOperationException("Could not determine active profile.");
        if (doc.Root?.Element("profiles")?.Element(activeProfile) == null)
        {
            throw new InvalidOperationException("The active profile, specified in <general> section, does not exist in <profiles> section.");
        }

        var apiToken = doc.Root?.Element("general")?.Element("token")?.Value?.Trim() ??
            throw new InvalidOperationException("Could not determine API token in general section.");

        var folder = doc.Root?.Element("profiles")?.Element(activeProfile)?.Element("folder")?.Value?.Trim() ??
            throw new InvalidOperationException("Could not determine target folder for given profile.");
        var targetFolder = Environment.ExpandEnvironmentVariables(folder);

        var addons = doc.Root?.Element("profiles")?.Element(activeProfile)?.Element("addons") ??
            throw new InvalidOperationException("Could not determine addon urls for given profile.");
        var addonUrls = addons.Elements()?.Where(e => e.Name == "url")?.Select(e => e.Value.Trim().ToLower())?.Distinct() ?? [];

        return new ConfigData(apiToken, targetFolder, addonUrls);
    }

    private static void CheckBasicFileStructure(XDocument doc)
    {
        var root = doc.Root;

        if (root == null || root.Name != "wowcam")
        {
            throw new InvalidOperationException("The <wowcam> root element does not exist.");
        }

        if (root.Element("general") == null)
        {
            throw new InvalidOperationException("The <general> section does not exist.");
        }

        var profiles = root.Element("profiles") ??
            throw new InvalidOperationException("The <profiles> section does not exist.");

        if (!profiles.HasElements)
        {
            throw new InvalidOperationException("The <profiles> section does not contain any profiles.");
        }
    }
}
