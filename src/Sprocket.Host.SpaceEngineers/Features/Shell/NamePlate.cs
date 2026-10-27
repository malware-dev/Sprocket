using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Lumel.Styling;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A name that can be given in place: its words with a pencil, which a press turns into a box holding the name.</summary>
    /// <remarks>The editor's chrome runs the renaming itself (<see cref="SprocketEditor.Rename" />), alike for a heading and a row.</remarks>
    public partial class NamePlate : HStack
    {
        /// <summary>How long a name in a row may be: the most the game's own text boxes in the terminal take.</summary>
        public const int NameLongest = 512;

        readonly bool _title;
        bool _emphasized;
        bool _renaming;
        bool _canRename;
        string _about;

        /// <summary>A plate in a heading's size where <paramref name="title" />, and in a row's otherwise.</summary>
        public NamePlate(bool title)
        {
            _title = title;
            CreateView();
        }

        /// <summary>The words the plate shows while the name is not being typed.</summary>
        public string Text
        {
            get { return _name.Text; }
            set
            {
                _name.Text = value;
                AutomationId = value;
            }
        }

        /// <summary>The style the words are shown in where they are a heading's rather than a name's.</summary>
        public IStyle<Label> Words
        {
            set { _name.Style = value; }
        }

        /// <summary>Whether the words are in italics, for names shown that are not the plate's own.</summary>
        public bool Emphasized
        {
            get { return _emphasized; }
            set
            {
                if (_emphasized == value)
                    return;

                _emphasized = value;
                _name.Set(LookStates.Emphasized, value);
            }
        }

        /// <summary>Whether the name can be given here, which shows the pencil.</summary>
        public bool CanRename
        {
            get { return _canRename; }
            set
            {
                _canRename = value;
                _pen.IsVisible = value;
            }
        }

        /// <summary>Whether the plate is drawn as a button, for a name a press opens a menu about rather than renames.</summary>
        public bool Boxed
        {
            get { return Style == Styles.PlateButton; }
            set { Style = value ? Styles.PlateButton : Styles.Plate; }
        }

        /// <summary>What the plate is and what pressing it does, said where the pointer rests on it while the name is not being typed, in markdown-lite; null for nothing.</summary>
        public string About
        {
            get { return _about; }
            set
            {
                _about = value;
                if (!_renaming)
                    Tip = Tip.For(Tip, value);
            }
        }

        /// <summary>The box the name is typed in, while it is being given.</summary>
        public TextBox Box => _box;

        /// <summary>Whether the name is being typed.</summary>
        public bool IsRenaming => _renaming;

        /// <summary>Puts <paramref name="name" /> in the box, all of it selected, in place of the words and the pencil's light.</summary>
        public void Begin(string name)
        {
            _renaming = true;
            _pen.Base.Tint = Palette.None;
            Tip = null;
            _name.IsVisible = false;
            _box.IsVisible = true;
            _box.Text = name;
            _box.SelectAll();
        }

        /// <summary>Shows the words again in place of the box.</summary>
        public void End()
        {
            _renaming = false;
            _pen.Base.Tint = Palette.Faint;
            _name.IsVisible = true;
            _box.IsVisible = false;
            Refuse(null);
            Tip = Tip.For(Tip, _about);
        }

        /// <summary>Marks the box as holding a name that cannot be given, saying <paramref name="why" /> on the box and the plate, or clears the mark for null.</summary>
        public void Refuse(string why)
        {
            _box.Set(LookStates.Wrong, why != null);
            _box.Tip = Tip.For(_box.Tip, why);
            Tip = Tip.For(Tip, why);
        }
    }
}
