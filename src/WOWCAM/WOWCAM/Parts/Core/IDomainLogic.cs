namespace WOWCAM.Parts.Core;

public interface IDomainLogic
{
    Task<DomainLogicResult> RunAsync(Action<IEnumerable<string>>? preflight = null, IProgress<byte>? progress = null, CancellationToken cancellationToken = default);
}
