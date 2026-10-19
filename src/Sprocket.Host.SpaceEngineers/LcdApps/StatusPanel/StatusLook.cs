using System;
using System.Collections.Generic;
using System.Text;
using Lumel;
using Lumel.Drawing;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>The size, colors and words the status panel is laid out in on one screen.</summary>
    public sealed class StatusLook
    {
        /// <summary>The smallest text drawn, in the screen's pixels.</summary>
        public const float Smallest = 16f;

        /// <summary>The smallest line of text drawn, in metres on the block, where the block's size is known.</summary>
        public const float Letter = 0.05f;

        /// <summary>The face every word on the panel is drawn in.</summary>
        public const string Face = MyFontEnum.White;

        readonly StringBuilder _measuring = new StringBuilder();

        /// <summary>The words the panel speaks in.</summary>
        public Texts Texts { get; private set; }

        /// <summary>The size the panel is drawn at on this screen.</summary>
        public PanelSize Size { get; private set; }

        /// <summary>The smallest a line of text may be on this screen.</summary>
        public float Floor { get; private set; }

        /// <summary>The panel's text color.</summary>
        public Color Ink { get; private set; }

        /// <summary>The panel's text color faded toward its background, as far as text can still be read on it.</summary>
        public Color Quiet { get; private set; }

        /// <summary>The panel's background color.</summary>
        public Color Ground { get; private set; }

        /// <summary>How far the cards are shown, from 0, faded into the background, to 1.</summary>
        public float Fade { get; set; } = 1f;

        /// <summary>How far a newly arrived installation's mark is lit toward the text color, from 0 to 1.</summary>
        public float Pulse { get; set; }

        /// <summary>The installations that newly want looking at, whose marks pulse.</summary>
        public List<StatusItem> Fresh { get; } = new List<StatusItem>();

        /// <summary><paramref name="color" /> as a card shows it, faded as far as <see cref="Fade" /> says.</summary>
        public Color Card(Color color)
        {
            return Fade >= 1f ? color : Color.Lerp(Ground, color, Fade);
        }

        /// <summary>Whether <paramref name="item" /> newly wants looking at.</summary>
        public bool IsFresh(StatusItem item)
        {
            foreach (var fresh in Fresh)
            {
                if (fresh.Same(item))
                    return true;
            }

            return false;
        }

        /// <summary>Takes the size for a screen <paramref name="room" /> big whose long side is <paramref name="metres" /> long, or 0 where that is not known, and the colors and words given.</summary>
        public void Take(Vector2 room, Texts texts, Color ink, Color ground, float metres)
        {
            var perMetre = metres > 0f ? Math.Max(room.X, room.Y) / metres : 0f;
            Floor = Math.Max(Smallest, Letter * perMetre);

            // The same size on both large programmable blocks: their screens are as wide as each other where one is taller,
            // so the width rules on the tall one and the height on the wide one.
            Size = PanelSize.For(Math.Min(room.Y / 13.33f, room.X / 17f), Floor);
            Texts = texts;
            Ink = ink;
            Ground = ground;
            Quiet = Contrast.Separate(Color.Lerp(ground, ink, 0.55f), ground, ink, Contrast.Text);
        }

        /// <summary>The size whose words fill <paramref name="high" /> of a bar's height, and no smaller than the floor.</summary>
        public PanelSize Banner(float high)
        {
            return PanelSize.For(high * 0.6f, Floor);
        }

        /// <summary>How wide <paramref name="text" /> is at <paramref name="size" /> on <paramref name="graphics" />.</summary>
        public float Width(IGraphics graphics, string text, float size)
        {
            _measuring.Clear().Append(text);
            return graphics.MeasureText(Face, _measuring, size).X;
        }

        /// <summary>The color of <paramref name="level" />'s mark, moved toward the text color as far as it takes to be seen on the background.</summary>
        public Color Shade(StatusLevel level)
        {
            return Contrast.Separate(level == StatusLevel.Problem ? Palette.Problem : Palette.Notice, Ground, Ink, Contrast.Mark);
        }

        /// <summary>The mark of <paramref name="level" />: a cross for a problem, the warning mark for the rest.</summary>
        public static string Mark(StatusLevel level)
        {
            return level == StatusLevel.Problem ? Symbols.Close : Symbols.Warning;
        }

        /// <summary>How tall <paramref name="element" /> is laid out <paramref name="wide" /> across.</summary>
        public static float Tall(IGraphics graphics, Element element, float wide)
        {
            element.Measure(graphics, new Vector2(wide, float.PositiveInfinity));
            return element.DesiredSize.Y;
        }
    }
}
