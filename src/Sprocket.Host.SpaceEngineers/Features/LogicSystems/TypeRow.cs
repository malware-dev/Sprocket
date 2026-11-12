using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>A type on a logic system's State tab: its name, then the words before its choices and the choices, with a grip that carries it, a way to add a choice, and a bin.</summary>
    public partial class TypeRow : HStack, IReorderable, IDragged
    {
        /// <summary>A type with no name and no choices yet, each choice it comes to have shown on a plate of its own.</summary>
        public TypeRow()
        {
            CreateView();
            ChoiceChips = new KeyedChildren<string, NamePlate>(_choices.Children, delegate { return new NamePlate(false); });
        }

        /// <summary>The type's name, which is given in place.</summary>
        public NamePlate Name => _name;

        /// <summary>The words before the choices, saying what they are.</summary>
        public string Lead
        {
            get { return _lead.Text; }
            set { _lead.Text = value; }
        }

        /// <summary>Its choices, one plate each by name, in order, shown by whoever fills the row.</summary>
        public KeyedChildren<string, NamePlate> ChoiceChips { get; }

        /// <summary>The button that adds a choice.</summary>
        public Button AddChoice => _add;

        /// <summary>What pressing the bin does, which is removing the type, or null where it cannot be.</summary>
        public ICommand Removes
        {
            get { return _remove.Command; }
            set
            {
                _remove.Command = value;
                _remove.IsEnabled = value != null;
            }
        }

        /// <summary>What pressing the bin does, or why it cannot be pressed, said where the pointer rests on it.</summary>
        public string RemovesAbout
        {
            set { _remove.Tip = KeptTip.For(_remove.Tip, value); }
        }

        /// <summary>Lets the grip be taken where <paramref name="can" />, dimmed where not, saying <paramref name="about" /> where the pointer rests on it.</summary>
        public void Grips(bool can, string about)
        {
            _grip.CanDrag = can;
            _grip.Base.Color = can ? Palette.Faint : Palette.Dim;
            _grip.Tip = KeptTip.For(_grip.Tip, about);
        }

        /// <inheritdoc />
        public DragHandle DragHandle => _grip;

        /// <inheritdoc />
        public bool IsDragging { get; private set; }

        /// <inheritdoc />
        bool IDragged.IsDragging
        {
            set
            {
                IsDragging = value;
                RemarkLook.Ground(this, NoticeLevel.Info, value ? Palette.SlotFill : Palette.None);
            }
        }

        /// <inheritdoc />
        public int DraggedWithCount => 0;
    }
}
