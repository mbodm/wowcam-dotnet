namespace WOWCAM.Parts.Addons.Processing;

public interface IAddonsProcessing
{
    Task<uint> ProcessAddonsAsync(IEnumerable<string> addonNames, string workFolder, IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
}
