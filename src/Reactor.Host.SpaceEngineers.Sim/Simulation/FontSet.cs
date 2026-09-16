using Reactor.Host.SpaceEngineers.Layout;
using VRageMath;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// Hands a measurement to whichever font was asked for.
    /// </summary>
    /// <remarks>
    /// The game keeps every loaded font in one registry and picks by subtype, so a screen may name any of
    /// them on any call. This is the smallest thing that reproduces that: the letters, the symbols, and a
    /// rule for what happens when a name is neither.
    /// <para>
    /// An unknown name falls back to the text font rather than measuring zero, because zero is the answer
    /// that makes a layout collapse silently. A run in a font that is not there should come out the wrong
    /// width, not the wrong shape.
    /// </para>
    /// </remarks>
    public sealed class FontSet : IFontMetrics
    {
        readonly IFontMetrics _text;
        readonly IFontMetrics _symbols;

        public FontSet(IFontMetrics text, IFontMetrics symbols)
        {
            _text = text;
            _symbols = symbols;
        }

        public Vector2 MeasurePixels(string font, string text, float scale)
        {
            return For(font).MeasurePixels(font, text, scale);
        }

        public float LineHeightPixels(string font, float scale)
        {
            return For(font).LineHeightPixels(font, scale);
        }

        IFontMetrics For(string font)
        {
            return font == Symbols.Font && _symbols != null ? _symbols : _text;
        }
    }
}
