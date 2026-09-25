using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;

namespace Sprocket.Host.SpaceEngineers.Screen
{
    /// <summary>What a component view can ask of the screen it is drawn on.</summary>
    public interface IScreenHost
    {
        /// <summary>
        /// What the editor is editing, for the views that have words to put on it.
        /// </summary>
        /// <remarks>
        /// Core names blocks by id and never by name, because a name is read off this and what to say
        /// about one that is no longer here is this target's to phrase. So a view that renders a
        /// sentence needs the world the sentence is about.
        /// </remarks>
        GridWorld World { get; }

        /// <summary>Sends a number to core. Reordering is the one thing a view answers with a position.</summary>
        void SetNumber(int command, int value);

        /// <summary>Sends a switch's new state to core.</summary>
        void SetToggle(int command, bool on);

        /// <summary>Sends text to core.</summary>
        void SetText(int command, string text);

        /// <summary>Puts the caret in a box a view shows, as pressing the box would.</summary>
        void Take(TextBox box);
    }
}
