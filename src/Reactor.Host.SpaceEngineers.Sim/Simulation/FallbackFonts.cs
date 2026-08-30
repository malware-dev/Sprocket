using System.Globalization;
using Avalonia.Media;
using VRageMath;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// Text metrics good enough to lay out against, and honest about not being the game's.
    /// </summary>
    /// <remarks>
    /// The line height is real: 37 pixels at the reference height, read off the game's own
    /// <c>FontData</c>, which is an XNA bitmap font declaring <c>height="37" base="30" size="23"</c>. The
    /// widths are not - they come from whatever face the desktop has, stretched so its line height lands on
    /// that number. So a layout built here has the right number of lines in the right places, and every
    /// individual run is the wrong width by a few percent.
    /// <para>
    /// That is the lower rung deliberately, and the upper one is already scoped: the same font XML carries
    /// a per-glyph advance and a kerning table, and reading them gives measurement that is exact rather
    /// than close. Until that exists, anything asking whether text fits should be read with suspicion, and
    /// <see cref="IsExact" /> says so out loud rather than leaving it to be discovered.
    /// </para>
    /// </remarks>
    public sealed class FallbackFonts : IFontMetrics
    {
        /// <summary>From the game's FontData: the height of a line at scale 1, at the reference height.</summary>
        public const float GameLineHeight = 37f;

        /// <summary>The height the game's own UI, and its font scaling, are designed against.</summary>
        public const float GameReferenceHeight = 1080f;

        readonly Typeface _typeface;
        readonly ScreenSpaceDevice _device;

        double _heightPerSize;

        public FallbackFonts(ScreenSpaceDevice device, string family)
        {
            _device = device;
            _typeface = new Typeface(FontFamily.Parse(family));
        }

        /// <summary>Whether these measurements are the game's. They are not, and will not be until the
        /// glyph tables are read.</summary>
        public bool IsExact
        {
            get { return false; }
        }

        public Vector2 MeasurePixels(string font, string text, float scale)
        {
            if (string.IsNullOrEmpty(text))
                return new Vector2(0f, LineHeightPixels(font, scale));

            FormattedText formatted = Build(text, FontSizeFor(font, scale));
            return new Vector2((float)formatted.Width, (float)formatted.Height);
        }

        public float LineHeightPixels(string font, float scale)
        {
            return GameLineHeight * EffectiveScale(scale);
        }

        /// <summary>
        /// The scale the game would actually draw at, which is the one it was handed multiplied by its own
        /// HUD scale. Reproduced here rather than ignored, because the backend under test corrects for it
        /// and a simulator that quietly did not apply it would make that correction look like a bug.
        /// </summary>
        public float EffectiveScale(float scale)
        {
            float height = _device.SafeGuiArea.Height;
            float gameScale = height > 0f ? height / GameReferenceHeight : 1f;
            return scale * gameScale;
        }

        /// <summary>The point size that puts this face's line height on the game's.</summary>
        public double FontSizeFor(string font, float scale)
        {
            if (_heightPerSize <= 0d)
            {
                FormattedText probe = Build("Mg", 100d);
                _heightPerSize = probe.Height / 100d;
                if (_heightPerSize <= 0d)
                    _heightPerSize = 1d;
            }

            return LineHeightPixels(font, scale) / _heightPerSize;
        }

        public FormattedText Build(string text, double size)
        {
            return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                _typeface, size, Brushes.White);
        }
    }
}
