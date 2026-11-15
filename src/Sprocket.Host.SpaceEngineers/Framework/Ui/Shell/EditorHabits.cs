
namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>How somebody is working in an editor.</summary>
    /// <remarks>Kept for the game session and never saved; on a client it stays on the client.</remarks>
    public sealed class EditorHabits
    {
        /// <summary>Whether the list of what to do shows the block's own vocabulary rather than Sprocket's.</summary>
        public bool ShowsTerminal;

        /// <summary>Where the guide to rich text's window was last moved to, in UI units, or null where it has not been moved.</summary>
        public VRageMath.Vector2? GuideAt;

        /// <summary>
        /// Which source a selector's first column was last switched to, as the selector numbers its sources, for the selector of
        /// something new to open on; 0, blocks, until one is.
        /// </summary>
        public int Source;

        /// <summary>Which tab each logic system's screen was last left on, by the logic system's id, for the screen to open on again.</summary>
        public readonly System.Collections.Generic.Dictionary<int, int> Tabs = new System.Collections.Generic.Dictionary<int, int>();
    }
}
