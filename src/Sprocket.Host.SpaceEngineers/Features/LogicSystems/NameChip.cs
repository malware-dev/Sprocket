using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>A name on a logic system's State tab, a type's, a choice's or a state's: a button, with the box it is typed in while it is renamed.</summary>
    public partial class NameChip : HStack
    {
        /// <summary>How long a name may be: the most the game's own text boxes in the terminal take.</summary>
        public const int NameLongest = 512;

        string _text = "";

        /// <summary>A chip with no name yet.</summary>
        public NameChip()
        {
            CreateView();
        }

        /// <summary>The name it shows.</summary>
        public string Name
        {
            get { return _text; }
            set
            {
                _text = value ?? "";
                _words.Text = _text;
                _name.AutomationId = _text;
            }
        }

        /// <summary>The mark of what kind of thing it names, one of <see cref="Symbols" />, shown inside the button before the name; null for none.</summary>
        public string Mark
        {
            get { return _mark.IsVisible ? _mark.Symbol : null; }
            set
            {
                _mark.Symbol = value;
                _mark.IsVisible = value != null;
            }
        }

        /// <summary>What pressing the name does, or null where it cannot be changed.</summary>
        /// <remarks>Drawn as a button while it can be pressed, and as the name alone where it cannot.</remarks>
        public ICommand Opens
        {
            get { return _name.Command; }
            set
            {
                _name.Command = value;
                _name.Style = value != null ? Styles.SlotName : Styles.SlotNameStill;
            }
        }

        /// <summary>What pressing the name does, said where the pointer rests on it; null for nothing.</summary>
        public string OpensAbout
        {
            set { _name.Tip = Tip.For(_name.Tip, value); }
        }

        /// <summary>The box the name is typed in, while it is being renamed.</summary>
        public TextBox Box => _box;

        /// <summary>Whether the name is being typed.</summary>
        public bool IsRenaming => _box.IsVisible;

        /// <summary>Shows the box in place of the name, holding the name, for it to be typed over.</summary>
        public void Rename()
        {
            _box.Text = _text;
            _box.SelectAll();
            _box.IsVisible = true;
            _name.IsVisible = false;
        }

        /// <summary>Shows the name again in place of the box.</summary>
        public void Renamed()
        {
            _box.IsVisible = false;
            _name.IsVisible = true;
        }
    }
}
