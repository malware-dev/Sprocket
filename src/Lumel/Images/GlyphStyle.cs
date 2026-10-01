using Lumel.Styling;

namespace Lumel.Images
{
    /// <summary>How a symbol looks, and how it changes while hot and while it cannot be used.</summary>
    /// <remarks>Laid in that order, so a disabled symbol looks disabled whatever else it is.</remarks>
    public sealed class GlyphStyle : Style<GlyphState>
    {
        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public GlyphStyle()
        {
            Hot = Layer();
            Disabled = Layer();
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        public GlyphStyle(GlyphStyle basedOn) : base(basedOn)
        {
            Hot = Layer();
            Disabled = Layer();
        }

        /// <summary>What changes while a press would reach the symbol.</summary>
        public GlyphState Hot { get; }

        /// <summary>What changes while the symbol cannot be used.</summary>
        public GlyphState Disabled { get; }

        /// <inheritdoc />
        protected override void LayStates(Element element)
        {
            if (element.IsHot && element.IsEnabled)
                Lay(Hot, element);

            if (!element.IsEnabled)
                Lay(Disabled, element);
        }
    }
}
