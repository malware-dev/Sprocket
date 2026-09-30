using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Images;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>One member of a block as it is shown wherever it is named: its marks, then its words.</summary>
    public class MemberName : HStack, ITextual
    {
        readonly Glyph _waits;
        readonly Label _name;

        /// <summary>A member with no words and no marks yet.</summary>
        public MemberName()
        {
            Spacing = 8f;
            Children.Add(new Glyph { Symbol = Symbols.Finish, Style = Styles.Waits, IsVisible = false }.As(out _waits));
            Children.Add(new Label().As(out _name));
        }

        /// <summary>What the member is called.</summary>
        public string Name
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        /// <summary>Whether the member is something that has to happen, which a step waits for.</summary>
        public bool Waits
        {
            get { return _waits.IsVisible; }
            set { _waits.IsVisible = value; }
        }

        /// <summary>The note over the waits mark.</summary>
        public string WaitsTip
        {
            set { _waits.Tip = Tip.For(_waits.Tip, value); }
        }

        /// <summary>Whether the words take all the room given and run onto further lines where they are wider.</summary>
        public bool Wraps
        {
            get { return _name.Wraps; }
            set
            {
                _name.Wraps = value;
                _name.Placement = value ? Flex.Grow : null;
            }
        }

        /// <inheritdoc />
        public string Font
        {
            get { return _name.Font; }
            set { _name.Font = value; }
        }

        /// <inheritdoc />
        public float FontSize
        {
            get { return _name.FontSize; }
            set { _name.FontSize = value; }
        }

        /// <inheritdoc />
        public Color Color
        {
            get { return _name.Color; }
            set { _name.Color = value; }
        }
    }
}
