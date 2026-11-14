using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>One piece of a screen's heading: some words, and whether they are somebody's to change.</summary>
    public sealed class HeadingPart : Naming
    {
        string _lead = "";
        string _after = "";
        string _mark;
        string _markAbout;
        string _text;
        bool _emphasized;
        ICommand _opens;
        string _about;
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

        /// <summary>Whether the words are in italics: names this part shows of things it does not stand for, which it cannot rename.</summary>
        public bool Emphasized
        {
            get { return _emphasized; }
            set { Set(ref _emphasized, value); }
        }

        /// <summary>What pressing this part does where it cannot be renamed, such as opening what it names, or null for nothing.</summary>
        public ICommand Opens
        {
            get { return _opens; }
            set { Set(ref _opens, value); }
        }

        /// <summary>What this part is and what renaming or pressing it does, for whoever rests the pointer on it, in markdown-lite, or null for nothing to say.</summary>
        public string About
        {
            get { return _about; }
            set { Set(ref _about, value); }
        }

        /// <summary>Says this is part of the heading <paramref name="on" /> shows, which is told whenever it changes.</summary>
        public void Show(EditorScreen on)
        {
            _on = on;
        }

        /// <summary>Tells the screen showing this that its heading changed.</summary>
        protected override void Changed()
        {
            if (_on != null)
                _on.Touch();
        }
    }
}
