using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Styling;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>What a switch looks like in one state: its words, its thumb, and what the room inside it is filled with either way.</summary>
    /// <remarks><see cref="TextState.Color" /> is the words.</remarks>
    public class SwitchState : TextState
    {
        /// <summary>What the room inside is filled with while on, or null to leave it to the layers beneath.</summary>
        public Color? OnTrack { get; set; }

        /// <summary>What the room inside is filled with while off, or null to leave it to the layers beneath.</summary>
        public Color? OffTrack { get; set; }

        /// <summary>The color of the thumb, or null to leave it to the layers beneath.</summary>
        public Color? Thumb { get; set; }

        /// <summary>The room held clear round the word, or null to leave it to the layers beneath.</summary>
        public Thickness? WordPadding { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var control = element as Switch;
            if (control == null)
                return;

            control.OnTrack = VRageMath.Color.Transparent;
            control.OffTrack = VRageMath.Color.Transparent;
            control.Thumb = VRageMath.Color.White;
            control.WordPadding = new Thickness(0f);
        }

        /// <inheritdoc />
        public override void Lay(Element element)
        {
            base.Lay(element);

            var control = element as Switch;
            if (control == null)
                return;

            if (OnTrack.HasValue)
                control.OnTrack = OnTrack.Value;

            if (OffTrack.HasValue)
                control.OffTrack = OffTrack.Value;

            if (Thumb.HasValue)
                control.Thumb = Thumb.Value;

            if (WordPadding.HasValue)
                control.WordPadding = WordPadding.Value;
        }
    }
}
