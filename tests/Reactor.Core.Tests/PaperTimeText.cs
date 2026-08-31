using Reactor.Editing;

namespace Reactor.Tests;

/// <summary>
/// A second set of words for time, deliberately not the game's.
/// </summary>
/// <remarks>
/// Ticks, unconverted. The Space Engineers host says "3 seconds ago" from a table built once; this
/// says "180 ticks ago" and builds a string each time. Both are correct for their target, and a test
/// that could not tell them apart would not be testing the seam.
/// </remarks>
sealed class PaperTimeText : ITimeText
{
    public string Ago(int ticksAgo) => ticksAgo < 0 ? "" : ticksAgo + " ticks ago";

    public string WaitChoice(int ticks) => ticks + " ticks";
}
