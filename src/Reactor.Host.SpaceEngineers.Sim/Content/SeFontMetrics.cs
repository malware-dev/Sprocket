using System.Xml.Linq;
using Reactor.Sim.Simulation;
using VRageMath;

namespace Reactor.Sim.Content
{
    /// <summary>
    /// The game's own text measurements, read from the bitmap font it actually draws with.
    /// </summary>
    /// <remarks>
    /// Worth being exact about, because the simulator is self-consistent: it measures with whatever it
    /// draws with, so a wrong font still looks right here and only goes wrong in the game. Measured against
    /// this font's real advances, a desktop face was coming out between nine and seventeen percent narrow,
    /// which is the difference between a row that fits and a row that runs off the panel.
    /// <para>
    /// The numbers are integers baked by BMFontGen from <c>space_engineers.ttf</c> at size 23, which is why
    /// they cannot be re-derived from the TrueType file: rounding at bake time and the game's own
    /// per-character spacing are both outside what the outline knows.
    /// </para>
    /// </remarks>
    public sealed class SeFontMetrics : IFontMetrics
    {
        /// <summary>
        /// Added to every glyph's advance, as the game does. Seven percent of a long row on its own, and
        /// nowhere in the font file - it lives in the game's renderer. This is the first number to suspect
        /// if measurements ever come out uniformly wrong.
        /// </summary>
        public const int Spacing = 1;

        /// <summary>The reference height the game's HUD scaling is defined against.</summary>
        public const float GameReferenceHeight = 1080f;

        /// <summary>Where one glyph sits on the page, and what it costs to draw.</summary>
        public struct Glyph
        {
            /// <summary>The cell on the font page, padding included.</summary>
            public int X;
            public int Y;
            public int Width;
            public int Height;

            /// <summary>How far the pen moves afterwards, before the game's own spacing.</summary>
            public int Advance;

            /// <summary>
            /// How far left of the pen the cell starts. Always -7 in this font, because the baker padded
            /// every glyph by seven so that neighbouring cells could not bleed into one another.
            /// </summary>
            public int Bearing;
        }

        /// <summary>
        /// How far down each cell the baseline sits, in font units.
        /// </summary>
        /// <remarks>
        /// Measured off the page rather than taken from the file, because the file does not say: it
        /// gives a line height of 37, a baseline of 30 and cells of 45, and no combination of those
        /// yields the vertical padding. Every flat-bottomed glyph in the font - H E L A O x n u v w -
        /// has its last row of ink at 34 and nothing at 35, which pins it exactly.
        /// <para>
        /// So a cell is drawn five units above the top of its line. To check it again: crop any of those
        /// glyphs out of the page and look at where the alpha stops.
        /// </para>
        /// </remarks>
        public const int GlyphBaselineInCell = 35;

        /// <summary>The cell that measurement was made on, so another font can be scaled against it.</summary>
        public const int GameCellHeight = 45;

        readonly Dictionary<char, Glyph> _glyphs = new Dictionary<char, Glyph>();
        readonly Dictionary<char, int> _advances = new Dictionary<char, int>();
        readonly Dictionary<long, int> _kerning = new Dictionary<long, int>();
        readonly ScreenSpaceDevice _device;
        readonly int _lineHeight;
        readonly int _base;

        /// <summary>How tall a cell on this page is, taken from the first glyph that says.</summary>
        int _cellHeight;

        SeFontMetrics(ScreenSpaceDevice device, int lineHeight, int baseline, string source, bool fromGame)
        {
            _device = device;
            _lineHeight = lineHeight;
            _base = baseline;
            Source = source;
            FromGame = fromGame;
        }

        /// <summary>Which page the glyphs are on, as the font file names it.</summary>
        public string PageFile { get; private set; }

        /// <summary>
        /// How far above the top of a line a glyph's cell begins, in font units. Negative, always.
        /// </summary>
        public int CellTop => _base - BaselineInCell;

        /// <summary>
        /// Where the baseline falls inside this font's cell, in its own units.
        /// </summary>
        /// <remarks>
        /// In the game's proportion rather than as a flat constant, because a constant is only right for
        /// the cell it was measured on. A font with a bigger cell has its baseline further down it, and
        /// treating 35 as universal put this project's own symbols six pixels below the words they stand
        /// beside - which reads as the artwork being wrong rather than the arithmetic.
        /// </remarks>
        public int BaselineInCell =>
            _cellHeight <= 0 || _cellHeight == GameCellHeight
                ? GlyphBaselineInCell
                : (int)System.Math.Round(_cellHeight * (double)GlyphBaselineInCell / GameCellHeight);

        /// <summary>Where <paramref name="c" /> is on the page, if the game has it at all.</summary>
        public bool TryGlyph(char c, out Glyph glyph)
        {
            return _glyphs.TryGetValue(c, out glyph);
        }

        /// <summary>What the game draws in place of a character it does not have.</summary>
        public bool TryReplacement(out Glyph glyph)
        {
            return _glyphs.TryGetValue(Replacement, out glyph);
        }

        /// <summary>How much a kerned pair pulls together, in font units. Zero for most pairs.</summary>
        public int Kerning(char left, char right)
        {
            int adjust;
            return _kerning.TryGetValue(Key(left, right), out adjust) ? adjust : 0;
        }

        public bool IsExact => true;

        /// <summary>Where the numbers came from, for the line in the window that says how true this is.</summary>
        public string Source { get; private set; }

        /// <summary>Whether they were read from an installed game rather than from the cache.</summary>
        public bool FromGame { get; private set; }

        /// <summary>The height of a line in font units, which is what every advance is relative to.</summary>
        public int FontLineHeight => _lineHeight;

        /// <summary>
        /// Loads the font the game's white UI text uses, from the game or the cache. Null when neither has
        /// it, which is a thing to fall back from rather than to fail on.
        /// </summary>
        public static SeFontMetrics Load(ScreenSpaceDevice device)
        {
            string path = GameContent.Resolve(Path.Combine("Fonts", "white", "FontDataPA.xml"));
            if (path == null)
                return null;

            return LoadFrom(device, path, GameContent.HasGame ? "the game" : "cache", GameContent.HasGame);
        }

        /// <summary>
        /// Reads any XNA bitmapfont, not only the game's.
        /// </summary>
        /// <remarks>
        /// Split out when this project started shipping a font of its own. The format is the same one
        /// either way - it is what the game reads - so a second reader would have been a second place for
        /// the same parsing to be subtly different.
        /// </remarks>
        public static SeFontMetrics LoadFrom(ScreenSpaceDevice device, string path, string source, bool live)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            try
            {
                XDocument document;
                using (var reader = new StreamReader(path, true))
                    document = XDocument.Load(reader);

                XElement font = document.Root;
                if (font == null)
                    return null;

                XNamespace ns = font.Name.Namespace;
                int lineHeight = (int?)font.Attribute("height") ?? 37;
                int baseline = (int?)font.Attribute("base") ?? 30;

                var metrics = new SeFontMetrics(device, lineHeight, baseline, source, live);

                XElement page = font.Descendants(ns + "bitmap").FirstOrDefault();
                metrics.PageFile = page == null ? null : (string)page.Attribute("name");

                foreach (XElement glyph in font.Descendants(ns + "glyph"))
                {
                    string character = (string)glyph.Attribute("ch");
                    int? advance = (int?)glyph.Attribute("aw");

                    if (string.IsNullOrEmpty(character) || !advance.HasValue)
                        continue;

                    metrics._advances[character[0]] = advance.Value;

                    Glyph cell;
                    if (ReadCell(glyph, advance.Value, out cell))
                    {
                        metrics._glyphs[character[0]] = cell;

                        if (metrics._cellHeight == 0)
                            metrics._cellHeight = cell.Height;
                    }
                }

                if (metrics._advances.Count == 0)
                    return null;

                foreach (XElement pair in font.Descendants(ns + "kernpair"))
                {
                    string left = (string)pair.Attribute("left");
                    string right = (string)pair.Attribute("right");
                    int? adjust = (int?)pair.Attribute("adjust");

                    if (!string.IsNullOrEmpty(left) && !string.IsNullOrEmpty(right) && adjust.HasValue)
                        metrics._kerning[Key(left[0], right[0])] = adjust.Value;
                }

                return metrics;
            }
            catch (Exception error) when (error is IOException || error is System.Xml.XmlException)
            {
                return null;
            }
        }

        /// <summary>
        /// What the game answers when asked how big a string is.
        /// </summary>
        /// <remarks>
        /// <b>The cell, not the line, and the two are not the same number.</b> <c>MyFont.MeasureString</c>
        /// walks the glyphs and takes the tallest, and in this face every glyph is a uniform 45 units in a
        /// line of 37 - so a measured string comes back a fifth taller than the line it will be drawn on,
        /// for any text at all.
        /// <para>
        /// This returned the line height for both, which made the difference invisible here and cost two
        /// wrong diagnoses of the same misalignment: anything laid out against a measured height sat
        /// correctly in the simulator and wrongly in the game, and the simulator agreed with every guess.
        /// A harness that cannot be wrong about the thing being tested is not a harness.
        /// </para>
        /// </remarks>
        public Vector2 MeasurePixels(string font, string text, float scale)
        {
            float perUnit = EffectiveScale(scale);
            float height = (_cellHeight > 0 ? _cellHeight : GameCellHeight) * perUnit;

            if (string.IsNullOrEmpty(text))
                return new Vector2(0f, height);

            return new Vector2(WidthInUnits(text) * perUnit, height);
        }

        public float LineHeightPixels(string font, float scale)
        {
            return _lineHeight * EffectiveScale(scale);
        }

        /// <summary>
        /// What the game draws in place of a character it does not have.
        /// </summary>
        /// <remarks>
        /// The font declares it, at U+25A1, which is how we know this is what happens rather than nothing
        /// happening.
        /// </remarks>
        /// <summary>Left alone, since a line break is not a character the font is asked for.</summary>
        public const char Newline = (char)10;

        public const char Replacement = '□';

        /// <summary>Whether the game can actually draw this character.</summary>
        public bool Has(char c)
        {
            return _advances.ContainsKey(c);
        }

        /// <summary>
        /// The run as the game would actually show it, with anything it cannot draw replaced.
        /// </summary>
        /// <remarks>
        /// The simulator measures with the game's numbers and draws with a desktop face, which is fine for
        /// shape and wrong for coverage: a desktop face has glyphs the game has never heard of, so a
        /// character the game would show as a box was quietly rendering perfectly here. Substituting makes
        /// the window admit it.
        /// </remarks>
        public string AsDrawn(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            char[] copy = null;

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == Newline || Has(text[i]))
                    continue;

                if (copy == null)
                    copy = text.ToCharArray();

                copy[i] = Replacement;
            }

            return copy == null ? text : new string(copy);
        }

        /// <summary>The width of a run in font units, before any scaling.</summary>
        public int WidthInUnits(string text)
        {
            int total = 0;
            char previous = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                int advance;
                if (!_advances.TryGetValue(c, out advance))
                {
                    // The game draws a replacement box for anything it does not have, and a box still
                    // takes room. Skipping would make unknown text measure short instead of wide.
                    _advances.TryGetValue('□', out advance);
                }

                int kern;
                if (previous != '\0' && _kerning.TryGetValue(Key(previous, c), out kern))
                    total += kern;

                total += advance + Spacing;
                previous = c;
            }

            return total;
        }

        /// <summary>
        /// Pixels per font unit at the requested scale, which includes the game's own HUD scaling. The
        /// backend corrects for that scaling on the way out, so a simulator that did not apply it would
        /// make the correction look like a fault.
        /// </summary>
        public float EffectiveScale(float scale)
        {
            float height = _device.SafeGuiArea.Height;
            float hud = height > 0f ? height / GameReferenceHeight : 1f;
            return scale * hud;
        }

        /// <summary>
        /// Reads one glyph's place on the page. Answers false for anything malformed, which is treated
        /// as the font simply not having that character rather than as a reason to abandon the file.
        /// </summary>
        static bool ReadCell(XElement glyph, int advance, out Glyph cell)
        {
            cell = new Glyph();

            int[] origin = Pair((string)glyph.Attribute("origin"), ',');
            int[] size = Pair((string)glyph.Attribute("size"), 'x');

            if (origin == null || size == null)
                return false;

            cell.X = origin[0];
            cell.Y = origin[1];
            cell.Width = size[0];
            cell.Height = size[1];
            cell.Advance = advance;
            cell.Bearing = (int?)glyph.Attribute("lsb") ?? 0;
            return true;
        }

        /// <summary>Two numbers with something between them, which is how this file writes both.</summary>
        static int[] Pair(string text, char between)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            int at = text.IndexOf(between);
            if (at <= 0)
                return null;

            int first;
            int second;

            if (!int.TryParse(text.Substring(0, at), out first))
                return null;

            if (!int.TryParse(text.Substring(at + 1), out second))
                return null;

            return new[] { first, second };
        }

        static long Key(char left, char right)
        {
            return ((long)left << 16) | right;
        }
    }
}
