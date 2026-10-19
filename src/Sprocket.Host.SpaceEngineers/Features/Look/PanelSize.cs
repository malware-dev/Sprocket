using System.Collections.Generic;
using Lumel;
using Lumel.Images;
using Lumel.Text;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>One size the automation panel is drawn at: the styles of its words and marks, and the room between them.</summary>
    /// <remarks>
    /// A line's style pads it in from the top of its line to the top of its capitals and up from the bottom to its baseline,
    /// so the room between two lines is the room seen between them.
    /// </remarks>
    public sealed class PanelSize
    {
        /// <summary>Every size, smallest first, each named for its sentence's line.</summary>
        public static readonly IReadOnlyList<PanelSize> All = new[]
        {
            new PanelSize(16f, 24f, 16f, 8f, 8f, 8f, 8f),
            new PanelSize(20f, 32f, 16f, 12f, 8f, 8f, 8f),
            new PanelSize(24f, 36f, 20f, 12f, 12f, 12f, 12f),
            new PanelSize(28f, 44f, 20f, 16f, 12f, 12f, 12f),
            new PanelSize(32f, 48f, 24f, 16f, 12f, 16f, 12f),
            new PanelSize(40f, 60f, 28f, 20f, 16f, 20f, 16f),
            new PanelSize(48f, 72f, 36f, 24f, 16f, 24f, 16f),
            new PanelSize(64f, 96f, 48f, 32f, 20f, 32f, 24f),
            new PanelSize(80f, 120f, 60f, 40f, 24f, 40f, 32f)
        };

        PanelSize(float body, float big, float small, float underMark, float underLabel, float underName, float dot)
        {
            Body = body;
            Big = big;
            Small = small;
            UnderMark = underMark;
            UnderLabel = underLabel;
            UnderName = underName;
            Dot = dot;
            Beside = body >= 32f ? 8f : 4f;
            Between = Fours(body / 2f);
            Icon = Fours(small * 1.25f);
            BannerIcon = Fours(body * 1.25f);

            Sentence = Line(body);
            Name = Line(big);
            Label = Line(small);
            Mark = new GlyphStyle { Base = { Font = Symbols.Font, FontSize = big } };
            Check = new GlyphStyle { Base = { Font = Symbols.Font, FontSize = Fours(big * 1.5f) } };
            Count = new GlyphStyle { Base = { Font = Symbols.Font, FontSize = small } };
            Dots = new GlyphStyle { Base = { Font = Symbols.Font, FontSize = dot } };
            Plain = new TextStyle { Base = { Font = MyFontEnum.White, FontSize = small } };
            Banner = new TextStyle { Base = { Font = MyFontEnum.White, FontSize = body } };
            BannerMark = new GlyphStyle { Base = { Font = Symbols.Font, FontSize = body } };
        }

        /// <summary>A sentence's line.</summary>
        public float Body { get; }

        /// <summary>A name's line.</summary>
        public float Big { get; }

        /// <summary>A label's line, or a count's.</summary>
        public float Small { get; }

        /// <summary>The room under a mark, over the lines it heads.</summary>
        public float UnderMark { get; }

        /// <summary>The room under a label, over the name it labels.</summary>
        public float UnderLabel { get; }

        /// <summary>The room under a name, over what is said of it.</summary>
        public float UnderName { get; }

        /// <summary>How large a page dot is.</summary>
        public float Dot { get; }

        /// <summary>The room between a mark and the count beside it.</summary>
        public float Beside { get; }

        /// <summary>The room between the parts of a line: the icon and the heading, one count and the next, one page dot and the next.</summary>
        public float Between { get; }

        /// <summary>How large the icon is in the top line.</summary>
        public float Icon { get; }

        /// <summary>How large the icon is in a bar across a short screen.</summary>
        public float BannerIcon { get; }

        /// <summary>A sentence, from the top of its capitals to its baseline.</summary>
        public TextStyle Sentence { get; }

        /// <summary>A name, from the top of its capitals to its baseline.</summary>
        public TextStyle Name { get; }

        /// <summary>A label over a name, from the top of its capitals to its baseline.</summary>
        public TextStyle Label { get; }

        /// <summary>A line of the top line or a count, its whole line.</summary>
        public TextStyle Plain { get; }

        /// <summary>A word of a bar across a short screen, its whole line.</summary>
        public TextStyle Banner { get; }

        /// <summary>A mark of a bar across a short screen.</summary>
        public GlyphStyle BannerMark { get; }

        /// <summary>The mark of what a card shows.</summary>
        public GlyphStyle Mark { get; }

        /// <summary>The check of a construct with nothing wanting looking at.</summary>
        public GlyphStyle Check { get; }

        /// <summary>A mark beside a count.</summary>
        public GlyphStyle Count { get; }

        /// <summary>A page dot.</summary>
        public GlyphStyle Dots { get; }

        /// <summary>The largest size whose sentence's line is no taller than <paramref name="body" />, and no smaller than <paramref name="floor" />.</summary>
        public static PanelSize For(float body, float floor)
        {
            PanelSize found = null;
            foreach (var size in All)
            {
                if (size.Body >= floor && (found == null || size.Body <= body))
                    found = size;
            }

            return found ?? All[All.Count - 1];
        }

        /// <summary>A line of <paramref name="size" />, padded in to the top of its capitals and up to its baseline, to the nearest four.</summary>
        static TextStyle Line(float size)
        {
            var top = Fours(size * TextSizes.CapTop);
            var bottom = Fours(size * (1f - TextSizes.Baseline));
            return new TextStyle { Base = { Font = MyFontEnum.White, FontSize = size, Padding = new Thickness(0f, -top, 0f, -bottom) } };
        }

        static float Fours(float size)
        {
            return (float)System.Math.Round(size / 4f) * 4f;
        }
    }
}
