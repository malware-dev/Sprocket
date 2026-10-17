
namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>How somebody is working in an editor.</summary>
    /// <remarks>Kept for the game session and never saved; on a client it stays on the client.</remarks>
    public sealed class EditorHabits
    {
        /// <summary>Whether the list of what to do shows the block's own vocabulary rather than Sprocket's.</summary>
        public bool ShowsTerminal;

        /// <summary>Where the codex's window was last moved to, in UI units, or null where it has not been moved.</summary>
        public VRageMath.Vector2? CodexAt;
    }
}
