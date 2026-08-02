namespace WOWCAM.Parts.Addons.Processing;

public interface IAddonsProcessing
{
    Task<int> ProcessAddonsAsync(IEnumerable<string> addonNames, string workFolder, IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
}
