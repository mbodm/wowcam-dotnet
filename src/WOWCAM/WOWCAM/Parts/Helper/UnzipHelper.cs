using System.IO.Compression;

namespace WOWCAM.Parts.Helper;

public sealed class UnzipHelper
{
    public static Task<bool> ValidateZipFileAsync(string zipFile, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFile);

        // No need for a ThrowIfCancellationRequested() here, since Task.Run() cancels on its own (if the
        // task has not already started) and since the sync .ToList() method can not be cancelled anyway.

        return Task.Run(() =>
        {
            try
            {
                // Validate if zip file is readable (not corrupted) and contains content.
                // Call .ToList() on the archive entries to perform a full content read.

                using var zipArchive = ZipFile.OpenRead(zipFile);

                var hasContent = zipArchive.Entries.ToList().Count != 0;

                return hasContent;
            }
            catch
            {
                return false;
            }
        },
        cancellationToken);
    }

    public static Task ExtractZipFileAsync(string zipFile, string destFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFile);
        ArgumentException.ThrowIfNullOrWhiteSpace(destFolder);

        // No need for a ThrowIfCancellationRequested() here, since Task.Run() cancels on its own (if the
        // task has not already started) and since the sync method one-liner can not be cancelled anyway.

        return Task.Run(() => ZipFile.ExtractToDirectory(zipFile, destFolder, true), cancellationToken);
    }
}
