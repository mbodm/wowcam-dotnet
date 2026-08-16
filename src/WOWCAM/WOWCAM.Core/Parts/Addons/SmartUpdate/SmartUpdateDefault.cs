using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using WOWCAM.Core.Parts.Extensions;
using WOWCAM.Helper;

namespace WOWCAM.Core.Parts.Addons.SmartUpdate;

internal sealed class SmartUpdateDefault(string workFolder) : ISmartUpdate
{
    // Class-internal type
    private sealed record SmartUpdateData(string AddonName, string DownloadUrl, string FolderHash, string TimeStamp);

    // CTOR-injected fields
    private readonly string workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));

    // Class-internal fields
    private readonly ConcurrentDictionary<string, SmartUpdateData> dict = new();
    private readonly string rootFolder = Path.Combine(workFolder, "SmartUpdate");
    private readonly string xmlFile = Path.Combine(workFolder, "SmartUpdate", "SmartUpdate.xml");

    public async Task<int> LoadAsync(CancellationToken cancellationToken = default)
    {
        dict.Clear();

        if (!File.Exists(xmlFile))
        {
            return 0;
        }

        using var fileStream = File.OpenRead(xmlFile);

        XDocument doc;
        try
        {
            doc = await XDocument.LoadAsync(fileStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Could not load SmartUpdate file (the file is either empty or not a valid XML file).", e);
        }

        var root = doc.Element("wowcam") ?? throw new InvalidOperationException("Invalid SmartUpdate file (the <wowcam> root element not exists).");
        var parent = root.Element("smartupdate") ?? throw new InvalidOperationException("Invalid SmartUpdate file (the <smartupdate> section not exists).");

        var entries = parent.Elements("entry");
        foreach (var entry in entries)
        {
            var addonName = entry?.Attribute("addonName")?.Value ?? string.Empty;
            var downloadUrl = entry?.Attribute("downloadUrl")?.Value ?? string.Empty;
            var folderHash = entry?.Attribute("folderHash")?.Value ?? string.Empty;
            var changedAt = entry?.Attribute("changedAt")?.Value ?? string.Empty;

            if (string.IsNullOrWhiteSpace(addonName) || string.IsNullOrWhiteSpace(downloadUrl) || string.IsNullOrWhiteSpace(folderHash) || string.IsNullOrWhiteSpace(changedAt))
            {
                throw new InvalidOperationException("Invalid SmartUpdate file (the <smartupdate> section contains one or more invalid entries).");
            }

            var zipContentFolderPath = GetCachedAddonFolderPath(downloadUrl);
            if (!Directory.Exists(zipContentFolderPath))
            {
                throw new InvalidOperationException("Invalid SmartUpdate file (the XML file and the corresponding zip content folder are not in sync).");
            }

            //var actualFolderHash = await CreateZipContentFolderHashAsync(zipContentFolderPath, cancellationToken).ConfigureAwait(false);
            //if (actualFolderHash != folderHash)
            //{
            //    throw new InvalidOperationException("The existing zip content folder is corrupted (hash is not equal to hash in XML file).");
            //}

            if (!dict.TryAdd(addonName, new SmartUpdateData(addonName, downloadUrl, folderHash, changedAt)))
            {
                throw new InvalidOperationException("Invalid SmartUpdate file (the <smartupdate> section contains multiple entries for the same addon.");
            }
        }

        return entries.Count();
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var entries = dict.OrderBy(kvp => kvp.Key).Select(kvp => new XElement("entry",
            new XAttribute("addonName", kvp.Key),
            new XAttribute("downloadUrl", kvp.Value.DownloadUrl),
            new XAttribute("folderHash", kvp.Value.FolderHash),
            new XAttribute("changedAt", kvp.Value.TimeStamp)));

        var doc = new XDocument(new XElement("wowcam", new XElement("smartupdate", entries)));

        using var fileStream = new FileStream(xmlFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true, IndentChars = "\t", NewLineOnAttributes = true, Async = true });
        await xmlWriter.FlushAsync().ConfigureAwait(false);
        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        await doc.SaveAsync(xmlWriter, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> AddonAlreadyExistsAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(addonName);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);

        if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null)
        {
            return false;
        }

        var hasExactAddonName = value.AddonName.Trim().Equals(addonName.Trim(), StringComparison.CurrentCultureIgnoreCase);
        var hasExactDownloadUrl = value.DownloadUrl.Trim().Equals(downloadUrl.Trim(), StringComparison.CurrentCultureIgnoreCase);
        var hasExactAddonVersion = hasExactAddonName && hasExactDownloadUrl;

        var cachedAddonFolder = GetCachedAddonFolderPath(value.DownloadUrl);
        var cachedAddonFolderExists = Directory.Exists(cachedAddonFolder);

        var cachedAddonFolderHash = CreateCachedAddonFolderHashAsync(value.DownloadUrl, cancellationToken).GetAwaiter().GetResult();
        var folderHashIsCorrect = cachedAddonFolderHash == value.FolderHash;

        return hasExactAddonVersion && cachedAddonFolderExists && folderHashIsCorrect;
    }

    public async Task AddOrUpdateEntryAsync(string addonName, string downloadUrl, string unzippedAddonSourceFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(addonName);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);

        var addonExists = await AddonAlreadyExistsAsync(addonName, downloadUrl, cancellationToken).ConfigureAwait(false);
        if (addonExists)
        {
            return;
        }

        // Remove old addon zip content folder (if it's an update)

        if (dict.ContainsKey(addonName))
        {
            if (dict.TryGetValue(addonName, out SmartUpdateData? value) && value != null)
            {
                if (!string.IsNullOrWhiteSpace(value.DownloadUrl))
                {
                    var oldCachedAddonFolder = GetCachedAddonFolderPath(value.DownloadUrl);
                    if (Directory.Exists(oldCachedAddonFolder))
                    {
                        Directory.Delete(oldCachedAddonFolder, true);
                    }
                }
            }
        }

        // Create new addon zip content folder

        var cachedAddonFolder = GetCachedAddonFolderPath(downloadUrl);
        if (!Directory.Exists(cachedAddonFolder))
        {
            Directory.CreateDirectory(cachedAddonFolder);
        }
        else
        {
            await FileSystemHelper.DeleteFolderContentAsync(cachedAddonFolder, cancellationToken).ConfigureAwait(false);
        }

        await FileSystemHelper.CopyFolderContentAsync(unzippedAddonSourceFolder, cachedAddonFolder, cancellationToken).ConfigureAwait(false);

        // Add new entry to dict

        var folderHash = await ComputeFolderHashAsync(cachedAddonFolder, cancellationToken).ConfigureAwait(false);
        var timeStamp = DateTime.UtcNow.ToIso8601();

        var dictValue = new SmartUpdateData(addonName, downloadUrl, folderHash, timeStamp);
        dict.AddOrUpdate(addonName, dictValue, (_, _) => dictValue);
    }

    public Task DeployExistingAddonAsync(string addonName, string destFolder, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private string GetCachedAddonFolderPath(string downloadUrl)
    {
        if (!Directory.Exists(rootFolder))
        {
            Directory.CreateDirectory(rootFolder);
        }

        var addonsFolder = Path.Combine(rootFolder, "Addons");
        if (Directory.Exists(addonsFolder))
        {
            Directory.CreateDirectory(addonsFolder);
        }

        var zipFileName = CurseHelper.GetZipFileNameFromAddonDownloadUrl(downloadUrl);
        var cachedAddonFolderName = Path.GetFileNameWithoutExtension(zipFileName);
        var cachedAddonFolderPath = Path.Combine(addonsFolder, cachedAddonFolderName);

        return cachedAddonFolderPath;
    }

    private async Task<string> CreateCachedAddonFolderHashAsync(string downloadUrl, CancellationToken cancellationToken = default)
    {
        var cachedAddonFolder = GetCachedAddonFolderPath(downloadUrl);
        if (!Directory.Exists(cachedAddonFolder))
        {
            // Todo: Throw
        }

        return await ComputeFolderHashAsync(cachedAddonFolder, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> ComputeFolderHashAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        // This pure hash method was created by Claude (for maximum performance)

        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException(folderPath);
        }

        // Collect relative paths (files + directories) to detect renames and moves as well as added/removed empty directories
        var relativeFilePaths = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(folderPath, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var relativeDirPaths = Directory.EnumerateDirectories(folderPath, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(folderPath, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        // Compute file hashes in parallel - result array preserves sorted order (index i maps to relativeFilePaths[i] value)
        var fileHashes = new byte[relativeFilePaths.Length][];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, relativeFilePaths.Length),
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            },
            async (i, ct) =>
            {
                var fullPath = Path.Combine(folderPath, relativeFilePaths[i]);
                await using var fileStream = File.OpenRead(fullPath);
                fileHashes[i] = await SHA256.HashDataAsync(fileStream, ct);
            });

        // Combine everything deterministically into a final hash
        using var combinedStream = new MemoryStream();

        foreach (var dirPath in relativeDirPaths)
        {
            var bytes = Encoding.UTF8.GetBytes("DIR:" + dirPath);
            combinedStream.Write(bytes);
        }

        for (var i = 0; i < relativeFilePaths.Length; i++)
        {
            var pathBytes = Encoding.UTF8.GetBytes("FILE:" + relativeFilePaths[i]);
            combinedStream.Write(pathBytes);
            combinedStream.Write(fileHashes[i]);
        }

        combinedStream.Position = 0;
        var finalHash = await SHA256.HashDataAsync(combinedStream, cancellationToken);

        return Convert.ToHexStringLower(finalHash);
    }
}
