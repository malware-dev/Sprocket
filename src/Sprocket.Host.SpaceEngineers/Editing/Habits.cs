namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>How somebody is working in an editor, as against what they are building in it.</summary>
    /// <remarks>
    /// Not saved with the program, and kept for as long as the block's editor is: the whole session
    /// where the block keeps its Sprocket, and handed from one copy to the next on a client, which
    /// opens a fresh copy every time. Nothing here goes to the server.
    /// </remarks>
    public sealed class Habits
    {
        /// <summary>How the picker for what starts a sequence was last narrowed.</summary>
        public readonly Filter When = new Filter();

        /// <summary>How the picker for what a step does was last narrowed.</summary>
        public readonly Filter Doing = new Filter();

        /// <summary>Whether the list of what to do shows the block's own vocabulary rather than Sprocket's.</summary>
        public bool ShowsTerminal;

        /// <summary>The filter for one of the two pickers.</summary>
        public Filter For(bool forEvents)
        {
            return forEvents ? When : Doing;
        }
    }
}
