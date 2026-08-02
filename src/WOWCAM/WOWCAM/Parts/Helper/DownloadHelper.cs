namespace WOWCAM.Parts.Helper;

public sealed class DownloadHelper
{
    private const long MaxFileSize = 2L * 1024 * 1024 * 1024; // 2 GB

    public static async Task DownloadFileAsync(HttpClient httpClient, string downloadUrl, string filePath,
        IProgress<DownloadProgress>? progress = default, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (progress == default)
        {
            using var response = await httpClient.GetAsync(downloadUrl, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var fileStream = File.Create(filePath);
            await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            fileStream.Close();
        }
        else
        {
            using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? throw new InvalidOperationException("Could not determine response content length.");
            if (totalBytes > MaxFileSize)
            {
                var maxMegaBytes = MaxFileSize / 1024.0 / 1024.0;
                throw new InvalidOperationException($"The size of the given file exceeds the maximum allowed size of {maxMegaBytes:F0} MB.");
            }

            progress.Report(new DownloadProgress(downloadUrl, true, 0, totalBytes, false));

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var fileStream = File.Create(filePath);

            // The buffer size is adjusted to the file's size as a performance improvement and for a consistent amount of progress steps for any file size
            // The buffer (and its size) is local to this method call (means: this code is safe even with a shared HttpClient across concurrent downloads)

            var progressSteps = 15;
            var bufferSize = (int)Math.Max(totalBytes / progressSteps, 1);
            var buffer = new byte[bufferSize];
            var readBytesNow = 0;
            var readBytesAll = 0L;

            while ((readBytesNow = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, readBytesNow), cancellationToken).ConfigureAwait(false);
                readBytesAll += readBytesNow;

                var transferFinished = readBytesAll >= totalBytes;
                progress.Report(new DownloadProgress(downloadUrl, false, readBytesAll, totalBytes, transferFinished));
            }

            if (readBytesAll != totalBytes)
            {
                throw new InvalidOperationException("Could not read exact same amount of bytes as predicted by content length.");
            }

            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            fileStream.Close();
        }
    }
}
