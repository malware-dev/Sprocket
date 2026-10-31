using System;
using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Localization;
using VRage.Utils;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>A construct's status laid out for one screen: a page with a top line and cards, or a bar on a screen too short for that.</summary>
    /// <remarks>
    /// <see cref="Root" /> is shown on a text panel, and <see cref="Lay" /> fills it, which the app does when the status, the
    /// page, the screen or its colors change. A page sliding in moves by <see cref="Slide" />, which lays nothing out.
    /// </remarks>
    public sealed class StatusPicture
    {
        /// <summary>The sprite the icon is drawn from.</summary>
        public const string IconSprite = "Sprocket_Icon";

        readonly StatusLook _look = new StatusLook();
        readonly StatusPage _page = new StatusPage();
        readonly StatusBar _bar = new StatusBar();
        readonly ZStack _root = new ZStack();
        bool _short;

        /// <summary>The size, colors, words and motion the next <see cref="Lay" /> lays out in.</summary>
        public StatusLook Look => _look;

        /// <summary>How many pages the screen last laid out takes turns between.</summary>
        public int Pages => _short ? 1 : _page.Pages;

        /// <summary>What the screen shows, on the screen's background, for a text panel to lay out and draw.</summary>
        public Element Root => _root;

        /// <summary>The icon, as a picture to draw.</summary>
        public static Sprite Icon()
        {
            return new Sprite(MyStringId.GetOrCompute(IconSprite), new Vector2(512f, 512f));
        }

        /// <summary>
        /// Lays <paramref name="status" /> out for <paramref name="graphics" />'s screen in <paramref name="texts" />, in
        /// <paramref name="ink" /> on <paramref name="ground" />, kept <paramref name="padding" /> hundredths of the screen in
        /// from its edges, on a screen whose long side is <paramref name="metres" /> long, or 0 where that is not known,
        /// showing the page <paramref name="turn" /> places in with the page <paramref name="from" /> places in beside it to
        /// slide from, as far slid as <see cref="StatusLook.Slid" /> says.
        /// </summary>
        /// <remarks><paramref name="graphics" /> is begun for the frame, so its size is the screen's.</remarks>
        public void Lay(IGraphics graphics, ConstructStatus status, Texts texts, Color ink, Color ground, float padding, float metres,
            int turn, int from)
        {
            var room = graphics.Size;
            _look.Take(room, texts, ink, ground, metres);

            var edge = (float)Math.Round(padding / 100f * Math.Min(room.X, room.Y));
            _short = room.Y < _look.Size.Body * 6f;
            Element shown;
            if (_short)
            {
                _bar.Lay(graphics, _look, status, room, edge);
                shown = _bar;
            }
            else
            {
                _page.Lay(graphics, _look, status, room, edge + _look.Size.Between, turn, from);
                shown = _page;
            }

            if (_root.Children.Count == 0 || _root.Children[0] != shown)
            {
                _root.Children.Clear();
                _root.Children.Add(shown);
            }

            _root.Base.Background = _look.Ground;
            Slide();
        }

        /// <summary>Moves the pages as far slid as <see cref="StatusLook.Slid" /> says, laying nothing out.</summary>
        public void Slide()
        {
            _page.Slid(_look.Slid);
        }
    }
}
