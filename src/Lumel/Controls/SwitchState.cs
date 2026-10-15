using VRageMath;
using Lumel.Styling;

namespace Lumel.Controls
{
    /// <summary>What a switch looks like in one state: its words, and its thumb and what the room inside it is filled with, either way.</summary>
    /// <remarks><see cref="TextState.Color" /> is the words.</remarks>
    public class SwitchState : TextState
    {
        /// <summary>What the room inside is filled with while on, or null to leave it to the layers beneath.</summary>
        public Color? OnTrack { get; set; }

        /// <summary>What the room inside is filled with while off, or null to leave it to the layers beneath.</summary>
        public Color? OffTrack { get; set; }

        /// <summary>The color of the thumb while on, or null to leave it to the layers beneath.</summary>
        public Color? OnThumb { get; set; }

        /// <summary>The color of the thumb while off, or null to leave it to the layers beneath.</summary>
        public Color? OffThumb { get; set; }

        /// <summary>The room held clear round the word, or null to leave it to the layers beneath.</summary>
        public Thickness? WordPadding { get; set; }

        /// <inheritdoc />
        public override void Neutral(Element element)
        {
            base.Neutral(element);

            var control = element as Switch;
            if (control == null)
                return;

            if (OnTrack.HasValue)
                control.OnTrack = VRageMath.Color.Transparent;

            if (OffTrack.HasValue)
                control.OffTrack = VRageMath.Color.Transparent;

            if (OnThumb.HasValue)
                control.OnThumb = VRageMath.Color.White;

            if (OffThumb.HasValue)
                control.OffThumb = VRageMath.Color.White;

            if (WordPadding.HasValue)
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

            if (OnThumb.HasValue)
                control.OnThumb = OnThumb.Value;

            if (OffThumb.HasValue)
                control.OffThumb = OffThumb.Value;

            if (WordPadding.HasValue)
                control.WordPadding = WordPadding.Value;
        }

        /// <inheritdoc />
        public override void Clear()
        {
            base.Clear();
            OnTrack = null;
            OffTrack = null;
            OnThumb = null;
            OffThumb = null;
            WordPadding = null;
        }

        /// <inheritdoc />
        public override void Gather(ElementState layer)
        {
            base.Gather(layer);
            var other = layer as SwitchState;
            if (other == null)
                return;

            if (!OnTrack.HasValue)
                OnTrack = other.OnTrack;

            if (!OffTrack.HasValue)
                OffTrack = other.OffTrack;

            if (!OnThumb.HasValue)
                OnThumb = other.OnThumb;

            if (!OffThumb.HasValue)
                OffThumb = other.OffThumb;

            if (!WordPadding.HasValue)
                WordPadding = other.WordPadding;
        }
    }
}
