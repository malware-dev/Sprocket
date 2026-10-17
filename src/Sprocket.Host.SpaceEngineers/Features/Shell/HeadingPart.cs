using System;
using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>One piece of a screen's heading: some words, and whether they are somebody's to change.</summary>
    public sealed class HeadingPart
    {
        string _lead = "";
        string _after = "";
        string _mark;
        string _markAbout;
        string _text;
        string _name = "";
        Action<string> _renames;
        Func<string, bool> _accepts;
        ICommand _opens;
        bool _namedOnArrival;
        string _about;
        bool _nameRequired;
        EditorScreen _on;

        /// <summary>A part reading <paramref name="text" />.</summary>
        public HeadingPart(string text)
        {
            _text = text ?? "";
        }

        /// <summary>Words that come before this part and belong to it, or empty for none: the <i>Automation for</i> of <i>Automation for Hangar Bay Alpha</i>.</summary>
        /// <remarks>A target puts its own mark between parts only where the next part has no lead.</remarks>
        public string Lead
        {
            get { return _lead; }
            set { Set(ref _lead, value ?? ""); }
        }

        /// <summary>Words after the name, quieter than it, or empty: the <i>blueprint</i> of <i>Airlock blueprint</i>.</summary>
        public string After
        {
            get { return _after; }
            set { Set(ref _after, value ?? ""); }
        }

        /// <summary>The symbol drawn before this part, saying what kind of thing it names, or null for none.</summary>
        public string Mark
        {
            get { return _mark; }
            set { Set(ref _mark, value); }
        }

        /// <summary>What kind of thing the mark says this part names, for whoever rests the pointer on it, or null for nothing to say.</summary>
        public string MarkAbout
        {
            get { return _markAbout; }
            set { Set(ref _markAbout, value); }
        }

        /// <summary>What this part reads as: the name given, or what the thing says about itself, never empty.</summary>
        public string Text
        {
            get { return _text; }
            set { Set(ref _text, value ?? ""); }
        }

        /// <summary>What somebody actually typed, or empty where nobody has.</summary>
        /// <remarks>A target opens its rename box on this.</remarks>
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value ?? ""); }
        }

        /// <summary>Gives this part a new name, or null when it cannot be renamed.</summary>
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

        /// <summary>Whether this part can be renamed.</summary>
        public bool CanRename => _renames != null;

        /// <summary>What pressing this part does where it cannot be renamed, such as opening what it names, or null for nothing.</summary>
        public ICommand Opens
        {
            get { return _opens; }
            set { Set(ref _opens, value); }
        }

        /// <summary>Whether the screen opens with this part's name in hand to be given, as for something just made and numbered.</summary>
        public bool NamedOnArrival
        {
            get { return _namedOnArrival; }
            set { Set(ref _namedOnArrival, value); }
        }

        /// <summary>What this part is and what renaming or pressing it does, for whoever rests the pointer on it, in markdown-lite, or null for nothing to say.</summary>
        public string About
        {
            get { return _about; }
            set { Set(ref _about, value); }
        }

        /// <summary>Whether an empty name is refused here, rather than handing the part back to what the thing says about itself.</summary>
        public bool NameRequired
        {
            get { return _nameRequired; }
            set { Set(ref _nameRequired, value); }
        }

        /// <summary>Whether <paramref name="name" /> can be given here.</summary>
        public bool Takes(string name)
        {
            return _accepts == null || _accepts(name ?? "");
        }

        /// <summary>Says this is part of the heading <paramref name="on" /> shows, which is told whenever it changes.</summary>
        public void Show(EditorScreen on)
        {
            _on = on;
        }

        /// <summary>Writes <paramref name="value" /> into <paramref name="field" />, telling the screen showing this where it differs.</summary>
        void Set<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            if (_on != null)
                _on.Touch();
        }
    }
}
