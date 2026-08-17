using Avalonia.Media;

namespace Reactor.Host.Rendering;

/// <summary>
/// What Reactor looks like on this target.
/// </summary>
/// <remarks>
/// Core hands over styles as intent - heading, muted, slot, problem - and never
/// colors or sizes. Every value here is this host's own opinion, and Space Engineers
/// is free to hold a completely different one without core knowing.
/// </remarks>
static class Palette
{
    public static readonly IBrush Page = new SolidColorBrush(Color.Parse("#12151a"));
    public static readonly IBrush Chrome = new SolidColorBrush(Color.Parse("#0d1117"));
    public static readonly IBrush Panel = new SolidColorBrush(Color.Parse("#0f1319"));
    public static readonly IBrush PanelHot = new SolidColorBrush(Color.Parse("#1a222c"));
    public static readonly IBrush Line = new SolidColorBrush(Color.Parse("#1f2630"));

    public static readonly IBrush Ink = new SolidColorBrush(Color.Parse("#e6edf7"));
    public static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#8b98a9"));
    public static readonly IBrush Faint = new SolidColorBrush(Color.Parse("#5a6675"));

    public static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#1f6feb"));
    public static readonly IBrush SlotEdge = new SolidColorBrush(Color.Parse("#3b82f6"));
    public static readonly IBrush SlotFill = new SolidColorBrush(Color.Parse("#1d2530"));

    public static readonly IBrush Live = new SolidColorBrush(Color.Parse("#4ade80"));
    public static readonly IBrush Dim = new SolidColorBrush(Color.Parse("#2a333f"));
    public static readonly IBrush LiveWash = new SolidColorBrush(Color.Parse("#16281c"));

    public static readonly IBrush Problem = new SolidColorBrush(Color.Parse("#f0776c"));
    public static readonly IBrush ProblemWash = new SolidColorBrush(Color.Parse("#2b1a1a"));
    public static readonly IBrush Notice = new SolidColorBrush(Color.Parse("#d4a72c"));
    public static readonly IBrush NoticeWash = new SolidColorBrush(Color.Parse("#2b2416"));
}
