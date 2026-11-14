using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>A name somebody may give in place, on a name plate: the name it holds, how a new one is given, and which it refuses.</summary>
    public class Naming
    {
        string _name = "";
        Action<string> _renames;
        Func<string, bool> _accepts;
        bool _namedOnArrival;
        bool _nameRequired;

        /// <summary>What somebody actually typed, or empty where nobody has.</summary>
        /// <remarks>The rename box opens holding this.</remarks>
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value ?? ""); }
        }

        /// <summary>Gives a new name, or null when the name cannot be changed.</summary>
        public Action<string> Renames
        {
            get { return _renames; }
            set { Set(ref _renames, value); }
        }

        /// <summary>Whether a name can be given, asked before it is: false where something else already has it.</summary>
        /// <remarks>Null where every name is accepted.</remarks>
        public Func<string, bool> Accepts
        {
            get { return _accepts; }
            set { Set(ref _accepts, value); }
        }

        /// <summary>Whether the name can be changed.</summary>
        public bool CanRename => _renames != null;

        /// <summary>Whether the name is in hand to be given as soon as it shows, as for something just made and numbered.</summary>
        public bool NamedOnArrival
        {
            get { return _namedOnArrival; }
            set { Set(ref _namedOnArrival, value); }
        }

        /// <summary>Whether an empty name is refused, rather than handing the name back to what the thing says about itself.</summary>
        public bool NameRequired
        {
            get { return _nameRequired; }
            set { Set(ref _nameRequired, value); }
        }

        /// <summary>Whether <paramref name="name" /> can be given.</summary>
        public bool Takes(string name)
        {
            return _accepts == null || _accepts(name ?? "");
        }

        /// <summary>Told whenever anything here changes value.</summary>
        protected virtual void Changed()
        {
        }

        /// <summary>Writes <paramref name="value" /> into <paramref name="field" />, saying so where it differs.</summary>
        protected void Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            Changed();
        }
    }
}
