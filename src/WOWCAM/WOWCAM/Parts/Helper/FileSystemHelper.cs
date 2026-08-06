namespace WOWCAM.Parts.Helper;

internal sealed class FileSystemHelper
{
    public static async Task DeleteFolderContentAsync(string folder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        if (!Directory.Exists(folder))
        {
            throw new InvalidOperationException("Given folder not exists.");
        }

        folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));

        // It does not matter which .NET methods are used to enumerate files and folders here.
        // At least this was the result of some measurements i did for the following methods:
        // - Directory.GetXXX()
        // - Directory.EnumerateXXX()
        // - DirectoryInfo.GetXXX()
        // - DirectoryInfo.EnumerateXXX()
        // All had the exact same performance and it does not matter which to use. If at all,
        // it is more beneficial to use directly the xxxFiles() and xxxDirectories() versions,
        // instead of the xxxFileSystemEntries() and xxxFileSystemInfos() methods. Cause the
        // latter ones need some additional if-clauses then, to differ files from directories.

        var dirs = Directory.EnumerateDirectories(folder);
        var files = Directory.EnumerateFiles(folder);

        if (!dirs.Any() && !files.Any())
        {
            return;
        }

        // After some measurements this async approach seems to be around 3 times faster than the
        // sync approach. Looks like modern SSD/OS configurations are rather concurrent-friendly.

        var tasks = new List<Task>();

        // No need for a ThrowIfCancellationRequested() here, since Task.Run() cancels on its own (if the
        // task has not already started) and since the sync method one-liner can not be cancelled anyway.

        tasks.AddRange(dirs.Select(dir => Task.Run(() => Directory.Delete(dir, true), cancellationToken)));
        tasks.AddRange(files.Select(file => Task.Run(() => File.Delete(file), cancellationToken)));

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Wait for deletion, as described at:
        // https://stackoverflow.com/questions/34981143/is-directory-delete-create-synchronous

        var counter = 0;

        while (Directory.EnumerateFileSystemEntries(folder).Any())
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            // Throw exception after ~500ms to prevent blocking forever.

            counter++;

            if (counter > 5)
            {
                throw new InvalidOperationException("Could not delete folder content.");
            }
        }
    }

    public static Task MoveFolderContentAsync(string sourceFolder, string destFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(destFolder);

        if (!Directory.Exists(sourceFolder))
        {
            throw new InvalidOperationException("Given source folder not exists.");
        }

        if (!Directory.Exists(destFolder))
        {
            Directory.CreateDirectory(destFolder);
        }

        sourceFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceFolder));
        destFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destFolder));

        var dirInfo = new DirectoryInfo(sourceFolder);
        var dirNames = dirInfo.GetDirectories().Select(dir => dir.Name);
        var fileNames = dirInfo.GetFiles().Select(file => file.Name);

        if (!dirNames.Any() && !fileNames.Any())
        {
            return Task.CompletedTask;
        }

        var dirTasks = dirNames.Select(dirName =>
        {
            var sourceDir = Path.Combine(sourceFolder, dirName);
            var destDir = Path.Combine(destFolder, dirName);

            // No need for a ThrowIfCancellationRequested() here, since Task.Run() cancels on its own (if the
            // task has not already started) and since the sync method one-liner can not be cancelled anyway.

            return Task.Run(() => Directory.Move(sourceDir, destDir), cancellationToken);
        });

        var fileTasks = fileNames.Select(fileName =>
        {
            var sourceFile = Path.Combine(sourceFolder, fileName);
            var destFile = Path.Combine(destFolder, fileName);

            // No need for a ThrowIfCancellationRequested() here, since Task.Run() cancels on its own (if the
            // task has not already started) and since the sync method one-liner can not be cancelled anyway.

            return Task.Run(() => File.Move(sourceFile, destFile), cancellationToken);
        });

        var allTasks = dirTasks.Concat(fileTasks);

        return Task.WhenAll(allTasks);
    }

    public static async Task CopyFolderContentAsync(string sourceFolder, string targetFolder, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetFolder);

        var overwrite = true;

        if (!Directory.Exists(sourceFolder))
        {
            throw new DirectoryNotFoundException($"The given source folder does not exist.");
        }

        foreach (var directory in Directory.EnumerateDirectories(sourceFolder, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceFolder, directory);
            Directory.CreateDirectory(Path.Combine(targetFolder, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(sourceFolder, file);
            var destinationFile = Path.Combine(targetFolder, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

            await using var sourceStream = File.OpenRead(file);
            await using var destStream = new FileStream(destinationFile, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);

            await sourceStream.CopyToAsync(destStream, cancellationToken).ConfigureAwait(false);
        }
    }
}
