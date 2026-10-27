using Lumel;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Styling;
using Lumel.Text;
using VRage.Game;
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
            Children.Add(new Glyph { Style = Styles.Waits, IsVisible = false, HaloSize = 20f * Sizes.Halo }.As(out _mark));
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
                if (!_mark.IsVisible)
                    return;

                _mark.Symbol = value;
                _mark.Halo = Symbols.OutlineOf(value);
            }
        }

        /// <summary>The color of the outline around its mark, transparent for none: tied to the wait or step under the pointer.</summary>
        public Color Glow
        {
            set { _mark.HaloTint = value; }
        }

        /// <summary>Whether its mark is as quiet as the words of a paused step.</summary>
        public bool Paused
        {
            set { _mark.Set(LookStates.Paused, value); }
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
        public string Font => _name.Font;

        /// <inheritdoc />
        public float FontSize => _name.FontSize;

        /// <inheritdoc />
        public Color Color => _name.Color;

        /// <summary>How this looks.</summary>
        public new IStyle<MemberName> Style
        {
            get { return (IStyle<MemberName>)StyleSet; }
            set { SetStyle(value); }
        }

        /// <summary>This member's own look whatever state it is in, laid over its style's.</summary>
        public new TextLook Base => (TextLook)base.Base;

        /// <inheritdoc />
        protected override bool Fits(IStyle style)
        {
            return style is IStyle<MemberName>;
        }

        /// <inheritdoc />
        protected override ElementLook NewLook()
        {
            return new TextLook();
        }

        /// <inheritdoc />
        protected override void Apply(ElementLook look)
        {
            base.Apply(look);

            var text = (TextLook)look;
            _name.Base.Wear(text.Font ?? MyFontEnum.White, text.FontSize ?? TextLook.NeutralFontSize, text.Color ?? Color.White);
        }
    }
}
