using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Images;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Look
{
    /// <summary>
    /// The pictures Sprocket's screens find by locator: a kind's or a button's mark for <c>mark:&lt;name&gt;</c>, and what the
    /// catalog it is given finds for anything else.
    /// </summary>
    /// <remarks>
    /// A mark is the symbol the screens show beside a thing of its kind, or on a button, so a page of rich text can show it
    /// beside a word that means that thing, or name the button by what it shows: <c>![signal](mark:signal)</c>,
    /// <c>![](mark:play)</c>. Each mark is made once and kept. A name with no mark is no picture, and the image says its
    /// words instead.
    /// </remarks>
    public sealed class MarkImages : IImageCatalog
    {
        const string Prefix = "mark:";

        readonly IImageCatalog _rest;
        readonly Dictionary<string, IImageSource> _marks = new Dictionary<string, IImageSource>(StringComparer.Ordinal);

        /// <summary>The marks, with <paramref name="rest" /> finding every locator that names none.</summary>
        public MarkImages(IImageCatalog rest)
        {
            _rest = rest;
            Add("sequence", Symbols.Sequence);
            Add("signal", Symbols.Signal);
            Add("trigger", Symbols.Trigger);
            Add("action", Symbols.Action);
            Add("awaited", Symbols.Lasting);
            Add("value", Symbols.Value);
            Add("block", Symbols.Block);
            Add("blueprint", Symbols.Blueprint);
            Add("logic-system", Symbols.LogicSystem);
            Add("installation", Symbols.Installation);
            Add("play", Symbols.Play);
            Add("stop", Symbols.Stop);
            Add("pause", Symbols.Pause);
            Add("delete", Symbols.Delete);
            Add("duplicate", Symbols.Duplicate);
            Add("rename", Symbols.Pencil);
            Add("undo", Symbols.Undo);
            Add("redo", Symbols.Redo);
        }

        /// <inheritdoc />
        public IImageSource Find(string locator)
        {
            if (string.IsNullOrEmpty(locator))
                return null;

            if (!locator.StartsWith(Prefix, StringComparison.Ordinal))
                return _rest?.Find(locator);

            IImageSource mark;
            return _marks.TryGetValue(locator, out mark) ? mark : null;
        }

        void Add(string name, string symbol)
        {
            _marks.Add(Prefix + name, new Mark(symbol));
        }

        /// <summary>One symbol, drawn in the color the screens draw marks in, three quarters the height of its line and centered in its room.</summary>
        sealed class Mark : IImageSource
        {
            /// <summary>How tall the symbol is drawn, as a part of its line.</summary>
            /// <remarks>Chosen by eye in the codex, Maltek Sans at 75%: a full line stood out over the letters beside it.</remarks>
            const float Height = 0.75f;

            readonly string _symbol;
            readonly StringBuilder _measuring;

            Vector2 _measured;
            float _measuredAt = -1f;

            public Mark(string symbol)
            {
                _symbol = symbol;
                _measuring = new StringBuilder(symbol);
            }

            /// <inheritdoc />
            /// <remarks>As tall as its line and as wide as the symbol is drawn, so the space either side of it is a word's.</remarks>
            public Vector2 Size => new Vector2(Height, 1f);

            /// <inheritdoc />
            /// <remarks>
            /// Measured again only where the room is a new height, which is once for every size of text it stands in. Drawn
            /// upright whatever its turn, since the screens draw text only upright.
            /// </remarks>
            public void Draw(IGraphics g, RectangleF destination, Color tint, float rotation)
            {
                var size = destination.Height * Height;
                if (size != _measuredAt)
                {
                    _measuredAt = size;
                    _measured = g.MeasureText(Symbols.Font, _measuring, size);
                }

                var ink = Palette.Muted;
                var color = new Color(ink.R * tint.R / 255, ink.G * tint.G / 255, ink.B * tint.B / 255, ink.A * tint.A / 255);
                g.DrawText(Symbols.Font, _symbol, new Vector2(
                    destination.X + (destination.Width - _measured.X) * 0.5f,
                    destination.Y + (destination.Height - _measured.Y) * 0.5f), size, color);
            }
        }
    }
}
