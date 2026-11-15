using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;

namespace Sprocket.Host.SpaceEngineers.Features.States
{
    /// <summary>
    /// A state on a logic system's State tab: its name, what it holds, the value it starts at, and what the installation
    /// shown holds now, with a grip that carries it and a bin.
    /// </summary>
    public partial class StateRow : HStack, IReorderable, IDragged
    {
        /// <summary>A state with no name yet.</summary>
        public StateRow()
        {
            CreateView();
        }

        /// <summary>The state's name, which is given in place.</summary>
        public NamePlate Name => _name;

        /// <summary>What it holds: a type's name, or the words for a flag.</summary>
        public string Kind
        {
            get { return _holds.Text; }
            set { _holds.Text = value ?? ""; }
        }

        /// <summary>The word before the value it starts at.</summary>
        public string StartsLead
        {
            set { _startsLead.Text = value ?? ""; }
        }

        /// <summary>The value it starts at, pressed to choose another.</summary>
        public Button Start => _start;

        /// <summary>The word before what the installation shown holds now.</summary>
        public string NowLead
        {
            set { _nowLead.Text = value ?? ""; }
        }

        /// <summary>What the installation shown holds now.</summary>
        public string Now
        {
            get { return _now.Text; }
            set { _now.Text = value ?? ""; }
        }

        /// <summary>What the installation shown holds now, pressed to change it while the installation or its logic system is switched off.</summary>
        public Button Current => _now;

        /// <summary>What the state holds, pressed to choose another type where it can be changed.</summary>
        public Button TypeButton => _holds;

        /// <summary>What pressing what the state holds does, which is choosing another type, or null where it cannot be changed.</summary>
        /// <remarks>Drawn as a button while it can be pressed, and as the words alone where it cannot.</remarks>
        public ICommand KindOpens
        {
            set
            {
                _holds.Command = value;
                _holds.Set(LookStates.Still, value == null);
            }
        }

        /// <summary>What the state holds, said where the pointer rests on it.</summary>
        public string KindAbout
        {
            set { _holds.Tip = CodexTip.For(_holds.Tip, value); }
        }

        /// <summary>What the value a state starts as is, said where the pointer rests on the words before it.</summary>
        public string StartsAbout
        {
            set { _startsLead.Tip = CodexTip.For(_startsLead.Tip, value); }
        }

        /// <summary>Said where the pointer rests on what the installation shown holds now.</summary>
        public string NowAbout
        {
            set { _now.Tip = CodexTip.For(_now.Tip, value); }
        }

        /// <summary>What pressing the bin does, which is removing the state, or null where it cannot be.</summary>
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
