using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
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
    private sealed record SmartUpdateData(string AddonName, string DownloadUrl, string TimeStamp);

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

        var root = doc.Element("wowcam")
            ?? throw new InvalidOperationException("Invalid SmartUpdate file (the <wowcam> root element does not exist).");
        var parent = root.Element("smartUpdate")
            ?? throw new InvalidOperationException("Invalid SmartUpdate file (the <smartUpdate> element does not exist).");
        var hash = parent.Element("cacheFolderHash")
            ?? throw new InvalidOperationException("Invalid SmartUpdate file (the <cacheFolderHash> element does not exist).");
        var entries = parent.Element("cacheEntries")
            ?? throw new InvalidOperationException("Invalid SmartUpdate file (the <cacheEntries> element does not exist).");

        var cacheFolderHashOld = hash.Value;
        var cacheFolderHashNow = await ComputeCacheFolderHashAsync(cancellationToken).ConfigureAwait(false);
        if (!cacheFolderHashOld.Trim().Equals(cacheFolderHashNow.Trim(), StringComparison.CurrentCultureIgnoreCase))
        {
            throw new InvalidOperationException("The SmartUpdate cache folder is corrupted (just delete SmartUpdate folder to solve this issue).");
        }

        var cacheEntryElements = entries.Elements("cacheEntry");
        foreach (var cacheEntry in cacheEntryElements)
        {
            var addonName = cacheEntry?.Element("addonName")?.Value;
            var downloadUrl = cacheEntry?.Element("downloadUrl")?.Value;
            var changedAt = cacheEntry?.Element("changedAt")?.Value;

            if (string.IsNullOrWhiteSpace(addonName) ||
                string.IsNullOrWhiteSpace(downloadUrl) ||
                string.IsNullOrWhiteSpace(changedAt))
            {
                throw new InvalidOperationException("Invalid SmartUpdate file (one or more <cacheEntry> elements are not valid).");
            }

            var cachedAddonFolder = GetCachedAddonFolderPath(downloadUrl);
            if (!Directory.Exists(cachedAddonFolder))
            {
                throw new InvalidOperationException("Invalid SmartUpdate file (one ore more <cacheEntry> elements are not in sync with the cache folder content).");
            }

            if (!dict.TryAdd(addonName, new SmartUpdateData(addonName, downloadUrl, changedAt)))
            {
                throw new InvalidOperationException("Invalid SmartUpdate file (the <cacheEntries> element contains multiple <cacheEntry> elements for the same addon).");
            }
        }

        return cacheEntryElements.Count();
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var cacheFolderHash = await ComputeCacheFolderHashAsync(cancellationToken).ConfigureAwait(false);

        var cacheEntries = dict.OrderBy(kvp => kvp.Key).Select(kvp => new XElement("addon",
            new XElement("name", kvp.Key),
            new XElement("downloadUrl", kvp.Value.DownloadUrl),
            new XElement("changedAt", kvp.Value.TimeStamp)));

        var doc = new XDocument(
            new XElement("wowcam",
                new XElement("smartUpdate",
                    new XElement("cache", cacheEntries,
                    new XAttribute("hash", cacheFolderHash)))));

        using var fileStream = new FileStream(xmlFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var xmlWriter = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true, IndentChars = "\t", Async = true });

        await xmlWriter.FlushAsync().ConfigureAwait(false);
        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);

        await doc.SaveAsync(xmlWriter, cancellationToken).ConfigureAwait(false);
    }

    public async Task CacheAddonAsync(string addonName, string downloadUrl, string unzippedAddonContentFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(addonName);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);

        var addonExists = await AddonAlreadyCachedAsync(addonName, downloadUrl, cancellationToken).ConfigureAwait(false);
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

        await FileSystemHelper.CopyFolderContentAsync(unzippedAddonContentFolder, cachedAddonFolder, cancellationToken).ConfigureAwait(false);

        // Add new entry to dict

        var dictValue = new SmartUpdateData(addonName, downloadUrl, DateTime.UtcNow.ToIso8601());
        dict.AddOrUpdate(addonName, dictValue, (_, _) => dictValue);
    }

    public async Task<bool> AddonAlreadyCachedAsync(string addonName, string downloadUrl, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(addonName);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);

        if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null)
        {
            return false;
        }

        var hasExactAddonName = value.AddonName.Trim().Equals(addonName.Trim(), StringComparison.CurrentCultureIgnoreCase);
        var hasExactDownloadUrl = value.DownloadUrl.Trim().Equals(downloadUrl.Trim(), StringComparison.CurrentCultureIgnoreCase);

        var cachedAddonFolder = GetCachedAddonFolderPath(value.DownloadUrl);
        var cachedAddonFolderExists = Directory.Exists(cachedAddonFolder);

        return hasExactAddonName && hasExactDownloadUrl && cachedAddonFolderExists;
    }

    public Task DeployCachedAddonAsync(string addonName, string destFolder, CancellationToken cancellationToken = default)
    {
        if (!dict.TryGetValue(addonName, out SmartUpdateData? value) || value == null)
        {
            throw new InvalidOperationException("SmartUpdate could not found an existing entry for given addon name.");
        }

        var cachedAddonFolder = GetCachedAddonFolderPath(value.DownloadUrl);
        if (!Directory.Exists(cachedAddonFolder))
        {
            throw new InvalidOperationException("SmartUpdate deployment failed, because the cache folder does not contain the addon.");
        }

        return FileSystemHelper.CopyFolderContentAsync(cachedAddonFolder, destFolder, cancellationToken);
    }

    private string GetCachedAddonFolderPath(string downloadUrl)
    {
        if (!Directory.Exists(rootFolder))
        {
            Directory.CreateDirectory(rootFolder);
        }

        var cacheFolder = Path.Combine(rootFolder, "Addons");
        if (Directory.Exists(cacheFolder))
        {
            Directory.CreateDirectory(cacheFolder);
        }

        var zipFileName = CurseHelper.GetZipFileNameFromAddonDownloadUrl(downloadUrl);
        var cachedAddonFolderName = Path.GetFileNameWithoutExtension(zipFileName);
        var cachedAddonFolderPath = Path.Combine(cacheFolder, cachedAddonFolderName);

        return cachedAddonFolderPath;
    }

    private async Task<string> ComputeCacheFolderHashAsync(CancellationToken cancellationToken = default)
    {
        var cacheFolder = GetCacheFolder();
        var cacheFolderHash = await ComputeFolderHashAsync(cacheFolder, cancellationToken).ConfigureAwait(false);

        return cacheFolderHash;
    }

    private string GetCacheFolder()
    {
        return Path.Combine(rootFolder, "Addons");
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
