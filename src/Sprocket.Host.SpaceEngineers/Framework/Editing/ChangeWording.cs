namespace Sprocket.Host.SpaceEngineers.Framework.Editing
{
    /// <summary>The words last put together for the change an arrow walks, and what they were put together from.</summary>
    public sealed class ChangeWording
    {
        object _change;
        object _screen;
        int _edits = -1;
        int _bound = -1;
        int _round = -1;

        /// <summary>The words.</summary>
        public string Text { get; set; } = "";

        /// <summary>
        /// Whether <see cref="Text" /> still says <paramref name="change" /> as walked on <paramref name="screen" />, the program
        /// having been edited <paramref name="edits" /> times, its names bound <paramref name="bound" /> times and the words read
        /// again <paramref name="round" /> times; where not, it is put down as saying it, for <see cref="Text" /> to be set.
        /// </summary>
        public bool Says(object change, object screen, int edits, int bound, int round)
        {
            if (change == _change && screen == _screen && edits == _edits && bound == _bound && round == _round)
                return true;

            _change = change;
            _screen = screen;
            _edits = edits;
            _bound = bound;
            _round = round;
            return false;
        }
    }
}
