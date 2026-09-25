using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Blueprints
{
    /// <summary>One part of a blueprint, and the blocks one of its logic systems fills it with.</summary>
    /// <remarks>
    /// The name and what it says about itself belong to the blueprint, so they read the same whichever
    /// logic system is chosen; only the blocks change. That difference, seen on one screen, is what
    /// tells a blueprint from the logic systems made from it.
    /// </remarks>
    public sealed class PartEntry : Observable
    {
        string _name;
        string _about = "";
        int _command;
        int _renameCommand;

        public PartEntry(string name)
        {
            _name = name ?? "";
        }

        /// <summary>What the part is called: the name its author gave it, or the one it was given.</summary>
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value ?? "", nameof(Name)); }
        }

        /// <summary>What the author wrote about the part beyond its name, or empty.</summary>
        public string About
        {
            get { return _about; }
            set { Set(ref _about, value ?? "", nameof(About)); }
        }

        /// <summary>The blocks the chosen logic system fills the part with, by entity id; none where it is empty.</summary>
        public List<long> Blocks { get; } = new List<long>();

        /// <summary>What pressing the blocks does: chooses others. Zero where they cannot be changed.</summary>
        public int Command
        {
            get { return _command; }
            set { Set(ref _command, value, nameof(Command)); }
        }

        /// <summary>What renaming the part is sent to, with the new name. Zero where it cannot be renamed.</summary>
        public int RenameCommand
        {
            get { return _renameCommand; }
            set { Set(ref _renameCommand, value, nameof(RenameCommand)); }
        }
    }
}
