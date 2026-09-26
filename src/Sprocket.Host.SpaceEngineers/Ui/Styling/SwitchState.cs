using Sprocket.Host.SpaceEngineers.Ui.Controls;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>What a switch looks like in one state: its words, its thumb, and what the room inside it is filled with either way.</summary>
    /// <remarks><see cref="TextState.Color" /> is the words.</remarks>
    public class SwitchState : TextState
    {
        Color? _offTrack;
        Color? _onTrack;
        Color? _thumb;
        Thickness? _wordPadding;

        /// <summary>What the room inside is filled with while on, or null to leave it to the layers beneath.</summary>
        public Color? OnTrack
        {
            get { return _onTrack; }
            set
            {
                Check();
                _onTrack = value;
            }
        }

        /// <summary>What the room inside is filled with while off, or null to leave it to the layers beneath.</summary>
        public Color? OffTrack
        {
            get { return _offTrack; }
            set
            {
                Check();
                _offTrack = value;
            }
        }

        /// <summary>The color of the thumb, or null to leave it to the layers beneath.</summary>
        public Color? Thumb
        {
            get { return _thumb; }
            set
            {
                Check();
                _thumb = value;
            }
        }

        /// <summary>The room held clear round the word, or null to leave it to the layers beneath.</summary>
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
            control.OnTrack = VRageMath.Color.Transparent;
            control.OffTrack = VRageMath.Color.Transparent;
            control.Thumb = VRageMath.Color.White;
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

            if (_onTrack.HasValue)
                control.OnTrack = _onTrack.Value;

            if (_offTrack.HasValue)
                control.OffTrack = _offTrack.Value;

            if (_thumb.HasValue)
                control.Thumb = _thumb.Value;

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

            _onTrack = state._onTrack;
            _offTrack = state._offTrack;
            _thumb = state._thumb;
            _wordPadding = state._wordPadding;
        }
    }
}
