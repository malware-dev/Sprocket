
namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>How somebody is working in an editor, as against what they are building in it.</summary>
    /// <remarks>
    /// Not saved with the program, and kept for as long as the block's editor is: the whole session
    /// where the block keeps its Sprocket, and handed from one copy to the next on a client, which
    /// opens a fresh copy every time. Nothing here goes to the server.
    /// </remarks>
    public sealed class Habits
    {
        /// <summary>Whether the list of what to do shows the block's own vocabulary rather than Sprocket's.</summary>
        public bool ShowsTerminal;
    }
}
