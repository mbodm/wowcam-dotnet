namespace WOWCAM.Parts.Addons.Processing;

// Encapsulates the entire progress handling for the IAddonsProcessor default implementation,
// so that class itself stays free of progress math and only calls a few descriptive methods.
//
// The whole idea in one paragraph:
//   Every addon goes through two equally-weighted stages: "Download (0..100)" and "Unzip".
//   We simply put each addon on a 0...200 points scale: 100 means "downloaded", and 200
//   means "downloaded & unzipped". With N addons, the biggest possible total is therefore
//   N * 200 (everything done). The overall percentage (to report) is then calculated by:
//
//   overallPercent = 100 * (sum of all addon values) / (N * 200)
//
//   One plain int slot per addon in a fixed-size array (index-aligned with the addon list).
//   Each concurrent addon task, in ProcessAddonsAsync() method, touches ONLY its own slot.
//
// Two things make the reported numbers correct and clean:
//   1)
//   Per-addon values only ever go UP (monotonic, via an atomic "raise to at least").
//   The download progress callbacks are delivered asynchronously and can arrive late,
//   or out of order. Without this: A late download report (e.g. 98) could overwrite
//   the "unzipped" mark (200) and the overall value would never reach the final 100%.
//   2)
//   The overall percentage is emitted at most ONCE per whole-percent step, counting up.
//   So the consumer sees "45, 46, 47, ..." instead of "45, 45, 45, 45, 46, 46, 47, ..."

public sealed class ProgressHelper
{
    private const int DownloadFinishedValue = 100;
    private const int UnzipFinishedValue = 200;

    private readonly IProgress<byte>? progress;
    private readonly int[] addonValueSlots;
    private readonly int maxTotalValue;

    private int lastReportedPercent;

    public ProgressHelper(int addonCount, IProgress<byte>? progress)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(addonCount);

        this.progress = progress;
        addonValueSlots = new int[addonCount];
        maxTotalValue = addonCount * UnzipFinishedValue;
    }

    // During the download -> downloadPercent (0..100) is this addon's value in stage 1
    public void ReportDownloadProgress(int addonIndex, byte downloadPercent) =>
        Advance(addonIndex, downloadPercent);

    // Download finished -> raise this addon's value to exactly 100
    public void ReportDownloadFinished(int addonIndex) =>
        Advance(addonIndex, DownloadFinishedValue);

    // Unzip finished -> raise this addon's value to exactly 200
    // Also covers the SmartUpdate case (which counts the same as download + unzip)
    public void ReportUnzipFinished(int addonIndex) =>
        Advance(addonIndex, UnzipFinishedValue);

    private void Advance(int addonIndex, int addonValue)
    {
        // This method raises this addon's slot (never lowers it),
        // and then emits every newly reached whole-percent step.

        RaiseToAtLeast(addonValueSlots, addonIndex, addonValue);

        var overallNow = CalcOverallPercent(addonValueSlots, maxTotalValue);

        // Emit every percent step between what was last reported and now, each exactly once.
        // Whichever addon task wins the atomic step-claim: This is the one that reports it.
        // So the concurrent addon tasks, in implementation, never emit the same number twice.

        int reported;
        while ((reported = Volatile.Read(ref lastReportedPercent)) < overallNow)
        {
            var next = reported + 1;
            if (Interlocked.CompareExchange(ref lastReportedPercent, next, reported) == reported)
            {
                progress?.Report((byte)next);
            }
        }
    }

    private static void RaiseToAtLeast(int[] slots, int index, int newValue)
    {
        // Sets slots[index] to newValue, only if that is higher than what is currently stored.
        // Lock-free and safe against concurrent (and out-of-order) callers, for the same slot.

        int current;
        do
        {
            current = Volatile.Read(ref slots[index]);
            if (newValue <= current)
            {
                // An equal-or-higher value is already stored -> Nothing to do

                return;
            }
        }
        while (Interlocked.CompareExchange(ref slots[index], newValue, current) != current);
    }

    private static byte CalcOverallPercent(int[] slots, int max)
    {
        // The "overallPercent" calculation, as an integer floor, clamped to [0..100].
        // Integer floor keeps the steps clean (no premature 100 from rounding).
        // It hits exactly 100, but only when every addon slot is at 200.

        if (max <= 0)
        {
            return 0;
        }

        // Each int read is atomic. A slightly "in-between" snapshot while other tasks write their
        // own slots is fine, because the value only ever climbs and the step-emitter catches up.

        long sum = 0;
        foreach (var value in slots)
        {
            sum += value;
        }

        var percent = (int)(sum * 100L / max);

        if (percent < 0) return 0;
        if (percent > 100) return 100;

        return (byte)percent;
    }
}
