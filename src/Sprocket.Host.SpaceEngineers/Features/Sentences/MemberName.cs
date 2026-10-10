using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>One member of a block as it is shown wherever it is named: the mark of its kind, then its words.</summary>
    public class MemberName : HStack, ITextual
    {
        readonly Glyph _mark;
        readonly Label _name;

        /// <summary>A member with no words and no mark yet.</summary>
        public MemberName()
        {
            Spacing = 8f;
            Children.Add(new Glyph { Style = Styles.Waits, IsVisible = false }.As(out _mark));
            Children.Add(new Label().As(out _name));
        }

        /// <summary>What the member is called.</summary>
        public string Name
        {
            get { return _name.Text; }
            set { _name.Text = value; }
        }

        /// <summary>The mark of what kind of member it is, one of <see cref="Symbols" />, or empty for none.</summary>
        public string Mark
        {
            get { return _mark.IsVisible ? _mark.Symbol : ""; }
            set
            {
                _mark.IsVisible = !string.IsNullOrEmpty(value);
                if (_mark.IsVisible)
                    _mark.Symbol = value;
            }
        }

        /// <summary>What the member says while the pointer rests on it, words and mark alike, or null for nothing.</summary>
        public string About
        {
            set { Tip = Tip.For(Tip, value); }
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
