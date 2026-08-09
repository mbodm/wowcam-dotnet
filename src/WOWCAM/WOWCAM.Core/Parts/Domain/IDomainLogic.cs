namespace WOWCAM.Core.Parts.Domain;

internal interface IDomainLogic
{
    Task<DomainLogicResult> RunAsync(IProgress<IEnumerable<string>>? preflight = default, IProgress<byte>? progress = default, CancellationToken cancellationToken = default);
}
