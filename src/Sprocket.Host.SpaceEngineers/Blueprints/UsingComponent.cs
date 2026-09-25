using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Blueprints
{
    /// <summary>
    /// Which logic system's blocks a blueprint is being edited through: "Using the blocks of Main airlock".
    /// </summary>
    /// <remarks>
    /// Over everything else on the screen, because every block named below it is that logic system's,
    /// and an edit here changes every logic system made from the blueprint. The name is pressed to
    /// choose another, which is how the same sentences are read against another airlock's doors.
    /// </remarks>
    public sealed class UsingComponent : EditorComponent
    {
        string _lead = "";
        string _name = "";
        int _command;

        /// <summary>The words that lead into the name.</summary>
        public string Lead
        {
            get { return _lead; }
            set { Set(ref _lead, value ?? "", nameof(Lead)); }
        }

        /// <summary>The logic system whose blocks are shown.</summary>
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value ?? "", nameof(Name)); }
        }

        /// <summary>What pressing the name does: chooses another. Zero where there is no other.</summary>
        public int Command
        {
            get { return _command; }
            set { Set(ref _command, value, nameof(Command)); }
        }
    }
}
