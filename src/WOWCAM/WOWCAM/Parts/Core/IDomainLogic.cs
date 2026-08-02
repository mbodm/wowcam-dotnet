namespace WOWCAM.Parts.Core;

public interface IDomainLogic
{
    Task<DomainLogicResult> RunAsync(Action<IEnumerable<string>>? preflight = default, IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
}
