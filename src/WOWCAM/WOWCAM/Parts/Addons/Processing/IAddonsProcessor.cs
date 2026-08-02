namespace WOWCAM.Parts.Addons.Processing;

public interface IAddonsProcessor
{
    Task<int> ProcessAddonsAsync(IEnumerable<string> addonNames, string workFolder, string targetFolder,
        IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
}
