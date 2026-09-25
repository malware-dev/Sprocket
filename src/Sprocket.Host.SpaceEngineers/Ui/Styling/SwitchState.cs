using VRage.Game;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a switch looks like in one state: its words, the colours either side, and the room round each word.</summary>
    /// <remarks><see cref="TextState.Color" /> is the word in force.</remarks>
    public class SwitchState : TextState
    {
        Color? _lit;
        Color? _quiet;
        Color? _unlit;
        Thickness? _wordPadding;

        /// <summary>The colour of a word not in force, or null to leave it to the layers beneath.</summary>
        public Color? QuietColor
        {
            get { return _quiet; }
            set
            {
                Check();
                _quiet = value;
            }
        }

        /// <summary>What the half in force is filled with, or null to leave it to the layers beneath.</summary>
        public Color? LitColor
        {
            get { return _lit; }
            set
            {
                Check();
                _lit = value;
            }
        }

        /// <summary>What the half not in force is filled with, or null to leave it to the layers beneath.</summary>
        public Color? UnlitColor
        {
            get { return _unlit; }
            set
            {
                Check();
                _unlit = value;
            }
        }

        /// <summary>The room held clear round each half's word, or null to leave it to the layers beneath.</summary>
        public Thickness? WordPadding
        {
            get { return _wordPadding; }
            set
            {
                Check();
                _wordPadding = value;
            }
        }

        /// <inheritdoc />
        internal override void Neutral(Element element)
        {
            base.Neutral(element);

            var control = element as Switch;
            if (control == null)
                return;

            control.Font = MyFontEnum.White;
            control.FontSize = NeutralFontSize;
            control.Color = VRageMath.Color.White;
            control.QuietColor = VRageMath.Color.White;
            control.LitColor = VRageMath.Color.Transparent;
            control.UnlitColor = VRageMath.Color.Transparent;
            control.WordPadding = new Thickness(0f);
        }

        /// <inheritdoc />
        internal override void Lay(Element element)
        {
            base.Lay(element);

            var control = element as Switch;
            if (control == null)
                return;

            if (Font != null)
                control.Font = Font;

            if (FontSize.HasValue)
                control.FontSize = FontSize.Value;

            if (Color.HasValue)
                control.Color = Color.Value;

            if (_quiet.HasValue)
                control.QuietColor = _quiet.Value;

            if (_lit.HasValue)
                control.LitColor = _lit.Value;

            if (_unlit.HasValue)
                control.UnlitColor = _unlit.Value;

            if (_wordPadding.HasValue)
                control.WordPadding = _wordPadding.Value;
        }

        /// <inheritdoc />
        internal override void Copy(ElementState other)
        {
            base.Copy(other);

            var state = other as SwitchState;
            if (state == null)
                return;

            _quiet = state._quiet;
            _lit = state._lit;
            _unlit = state._unlit;
            _wordPadding = state._wordPadding;
        }
    }
}
